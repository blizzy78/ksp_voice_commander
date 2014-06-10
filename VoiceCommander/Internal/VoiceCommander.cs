/*
Copyright (c) 2014, Maik Schreiber
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Toolbar;

namespace VoiceCommander {
	[KSPAddonFixed(KSPAddon.Startup.Instantly, true, typeof(VoiceCommander))]
	public partial class VoiceCommander : MonoBehaviour {
		internal const int VERSION = 4;

		private const string HOST = "127.0.0.1";
		private const int CLIENT_PORT = 48285;
		private const int SERVER_PORT = 48286;
		private const float MIN_CONFIDENCE = 0.6f;
		private const long PUSH_TO_TALK_GRACE_PERIOD = 1500;

		internal List<VoiceCommandNamespace> Namespaces {
			get;
			private set;
		}

		internal bool UpdateAvailable {
			get;
			private set;
		}

		private IPEndPoint clientEndPoint;
		private IPEndPoint serverEndPoint;
		private Settings settings;
		private UdpClient client;
		private List<VoicePacket> packets = new List<VoicePacket>();
		private IButton button;
		private bool listening = true;
		private bool pushToTalkListening;
		private long pushToTalkListeningStopped;
		private SettingsWindow settingsWindow;
		private Dictionary<string, string[]> macroValueTexts = new Dictionary<string, string[]>();
		private UpdateChecker updateChecker;

		private bool ReactToCommands {
			get {
				return settings.pushToTalk ? pushToTalkListening : listening;
			}
		}

		private bool IsInPushToTalkGracePeriod {
			get {
				if (pushToTalkListening) {
					return true;
				} else {
					long now = DateTime.UtcNow.Ticks / 10000;
					return (now - pushToTalkListeningStopped) < PUSH_TO_TALK_GRACE_PERIOD;
				}
			}
		}

		internal VoiceCommander() {
			Instance = this;

			Vessels = new Vessel[0];
			Namespaces = new List<VoiceCommandNamespace>();
		}

		private void Awake() {
			GameObject.DontDestroyOnLoad(this);

			settings = Settings.load();

			clientEndPoint = new IPEndPoint(IPAddress.Parse(HOST), CLIENT_PORT);
			serverEndPoint = new IPEndPoint(IPAddress.Parse(HOST), SERVER_PORT);
			client = new UdpClient(clientEndPoint);
			startReceive();

			Debug.Log("[VoiceCommander] now listening for commands from voice server");
		}

		private void Start() {
			button = ToolbarManager.Instance.add("VoiceCommander", "VoiceCommander");
			button.ToolTip = "Toggle Voice Commander (Right-Click for Settings)";
			button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.SPH, GameScenes.TRACKSTATION);
			button.OnClick += (e) => {
				if (e.MouseButton == 1) {
					toggleSettings();
				} else {
					toggleListen();
				}
			};
			updateButtonIcon();

			// close settings window on scene change
			GameEvents.onGameSceneLoadRequested.Add(sceneChanged);

			updateChecker = new UpdateChecker();
			updateChecker.OnDone += () => {
				UpdateAvailable = updateChecker.UpdateAvailable;
				updateChecker = null;
			};
		}

		private void updateButtonIcon() {
			button.TexturePath = ReactToCommands ? "blizzy/VoiceCommander/listening" : "blizzy/VoiceCommander/idle";
		}

		private void startReceive() {
			client.BeginReceive(dataReceived, null);
		}

		private void OnDestroy() {
			settings.save();

			client.Close();
			button.Destroy();
			Instance = null;
		}

		private void Update() {
			handleQueuedPackets();

			if (updateChecker != null) {
				updateChecker.update();
			}

			if (settingsWindow != null) {
				settingsWindow.update();
			}

			handlePushToTalk();
		}

		private void OnGUI() {
			if (settingsWindow != null) {
				settingsWindow.draw();
			}
		}

		private void handleQueuedPackets() {
			for (;;) {
				VoicePacket currentPacket = null;
				lock (packets) {
					if (packets.Count == 0) {
						break;
					}

					currentPacket = packets[0];
					packets.RemoveAt(0);
				}

				if (currentPacket != null) {
					handlePacket(currentPacket);
				}
			}
		}

		private void handlePacket(VoicePacket packet) {
			Debug.Log(string.Format("[VoiceCommander] packet received: ({0}) {1}", packet.Type, packet.Data));
			if (packet.Type == PacketType.SPEECH_RECOGNIZED) {
				handleSpeechRecognized(packet);
			}
		}

		private void handleSpeechRecognized(VoicePacket packet) {
			Debug.Log(string.Format("[VoiceCommander] command received: {0}", packet.Data));

			string[] parts = packet.Data.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, string> parameters = new Dictionary<string, string>();
			foreach (string part in parts) {
				string[] paramParts = part.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				parameters.Add(paramParts[0], paramParts[1]);
			}

			float confidence = float.Parse(parameters["confidence"]);
			if (confidence >= MIN_CONFIDENCE) {
				string command = parameters["command"];
				VoiceCommand cmd = findCommand(command);
				if (cmd != null) {
					bool react = ReactToCommands || (settings.pushToTalk && IsInPushToTalkGracePeriod);
					if ((react && !isGamePaused()) || cmd.ExecuteAlways) {
						button.Drawable = new InfoDrawable(button, string.Format("{0} ({1:F1}%)", cmd.Label, confidence * 100f));
						try {
							cmd.Callback(new VoiceCommandRecognizedEvent(parameters));
						} catch (Exception e) {
							VoiceCommandNamespace ns = Namespaces.FirstOrDefault(n => n.Commands.Contains(cmd));
							Debug.LogError(string.Format("[VoiceCommander] error while executing command: {0}/{1}", ns.Id, cmd.Id));
							Debug.LogException(e);
						}
					} else {
						Debug.Log(string.Format("[VoiceCommander] not listening, ignoring command: {0}", packet.Data));
					}
				} else {
					Debug.Log(string.Format("[VoiceCommander] unknown command received: {0}", packet.Data));
				}
			}
		}

		private bool isGamePaused() {
			if (HighLogic.LoadedSceneIsFlight) {
				return FlightDriver.Pause;
			} else {
				return false;
			}
		}

		private VoiceCommand findCommand(string fullCommandId) {
			string[] parts = fullCommandId.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			string nsId = parts[0];
			string cmdId = parts[1];

			foreach (VoiceCommandNamespace ns in Namespaces) {
				if (ns.Id == nsId) {
					foreach (VoiceCommand cmd in ns.Commands) {
						if (cmd.Id == cmdId) {
							return cmd;
						}
					}
				}
			}
			return null;
		}

		private void dataReceived(IAsyncResult result) {
			IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] data = client.EndReceive(result, ref senderEndPoint);
			try {
				VoicePacket packet = VoicePacket.FromPacket(data);
				lock (packets) {
					packets.Add(packet);
				}
			} catch (Exception) {
				Debug.LogWarning("[VoiceCommander] received malformed packet, ignoring");
			}
			startReceive();
		}

		private void updateCommandsOnServer() {
			sendPacketToServer(new VoicePacket(PacketType.CLEAR_COMMANDS));

			sendSpecialTextToServer(PacketType.SET_YAW_COMMAND, settings.yawText);
			sendSpecialTextToServer(PacketType.SET_PITCH_COMMAND, settings.pitchText);
			sendSpecialTextToServer(PacketType.SET_ROLL_COMMAND, settings.rollText);
			sendSpecialTextToServer(PacketType.SET_PROGRADE_COMMAND, settings.progradeText);
			sendSpecialTextToServer(PacketType.SET_RETROGRADE_COMMAND, settings.retrogradeText);
			sendSpecialTextToServer(PacketType.SET_NORMAL_COMMAND, settings.normalText);
			sendSpecialTextToServer(PacketType.SET_ANTI_NORMAL_COMMAND, settings.antiNormalText);
			sendSpecialTextToServer(PacketType.SET_RADIAL_COMMAND, settings.radialText);
			sendSpecialTextToServer(PacketType.SET_ANTI_RADIAL_COMMAND, settings.antiRadialText);
			sendSpecialTextToServer(PacketType.SET_APOAPSIS_COMMAND, settings.apoapsisText);
			sendSpecialTextToServer(PacketType.SET_PERIAPSIS_COMMAND, settings.periapsisText);
			sendSpecialTextToServer(PacketType.SET_MANEUVER_NODE_COMMAND, settings.maneuverNodeText);
			sendSpecialTextToServer(PacketType.SET_SOI_COMMAND, settings.soiText);

			foreach (KeyValuePair<string, List<string>> cmdEntry in settings.texts) {
				foreach (string text in cmdEntry.Value) {
					sendPacketToServer(new VoicePacket(PacketType.ADD_COMMAND, cmdEntry.Key + "|" + text));
				}
			}

			foreach (KeyValuePair<string, string[]> macroEntry in macroValueTexts) {
				foreach (string text in macroEntry.Value) {
					sendPacketToServer(new VoicePacket(PacketType.ADD_MACRO_COMMAND, macroEntry.Key + "|" + text));
				}
			}

			sendPacketToServer(new VoicePacket(PacketType.END_OF_COMMANDS));
		}

		private void sendSpecialTextToServer(PacketType type, string text) {
			if (!string.IsNullOrEmpty(text)) {
				sendPacketToServer(new VoicePacket(type, text));
			}
		}

		private void sendPacketToServer(VoicePacket packet) {
			byte[] data = packet.PacketData;
			client.Send(data, data.Length, serverEndPoint);
		}

		internal void toggleListen() {
			listening = !listening;
			updateButtonIcon();
		}

		private void sceneChanged(GameScenes scene) {
			settingsWindow = null;
		}

		private void toggleSettings() {
			if (settingsWindow != null) {
				settingsWindow = null;
			} else {
				bool oldPushToTalk = settings.pushToTalk;
				settingsWindow = new SettingsWindow(settings,
					() => {
						settingsWindow.saveToSettings();
						settingsWindow = null;

						// if push-to-talk has been switched off, activate regular listening mode to avoid confusion
						// ("why is it still not listening??")
						if (!settings.pushToTalk && oldPushToTalk) {
							listening = true;
						}

						updateButtonIcon();

						settings.save();

						updateCommandsOnServer();
					}, () => {
						settingsWindow = null;
					});
			}
		}

		private void handlePushToTalk() {
			bool newPushToTalkListening;
			if (Input.anyKey && (GUIUtility.keyboardControl == 0) && settings.pushToTalk && (settings.pushToTalkKey != KeyCode.None)) {
				newPushToTalkListening = Utils.getCurrentInputKey() == settings.pushToTalkKey;
			} else {
				newPushToTalkListening = false;
			}
			if (newPushToTalkListening != pushToTalkListening) {
				setPushToTalkListening(newPushToTalkListening);
			}
		}

		private void setPushToTalkListening(bool pushToTalkListening) {
			this.pushToTalkListening = pushToTalkListening;
			if (!pushToTalkListening) {
				pushToTalkListeningStopped = DateTime.UtcNow.Ticks / 10000;
			}
			updateButtonIcon();
		}
	}
}
