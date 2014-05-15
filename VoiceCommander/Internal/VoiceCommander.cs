/*
Copyright (c) 2013-2014, Maik Schreiber
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
	[KSPAddonFixed(KSPAddon.Startup.MainMenu, true, typeof(VoiceCommander))]
	public partial class VoiceCommander : MonoBehaviour {
		internal const int VERSION = 3;

		private static readonly string SETTINGS_FILE = KSPUtil.ApplicationRootPath + "GameData/blizzy/VoiceCommander/settings.dat";
		private const string HOST = "127.0.0.1";
		private const int CLIENT_PORT = 48285;
		private const int SERVER_PORT = 48286;
		private const float MIN_CONFIDENCE = 0.6f;

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
		private UdpClient client;
		private List<VoicePacket> packets = new List<VoicePacket>();
		private IButton button;
		private bool listening = true;
		private SettingsWindow settingsWindow;
		private Dictionary<string, List<string>> texts;
		private string yawText;
		private string pitchText;
		private string rollText;
		private string progradeText;
		private string retrogradeText;
		private string normalText;
		private string antiNormalText;
		private string radialText;
		private string antiRadialText;
		private string apoapsisText;
		private string periapsisText;
		private string maneuverNodeText;
		private string soiText;
		private Dictionary<string, string[]> macroValueTexts = new Dictionary<string, string[]>();
		private InternalCommands internalCommands = new InternalCommands();
		private KSPCommands kspCommands = new KSPCommands();
		private UpdateChecker updateChecker;

		internal VoiceCommander() {
			Instance = this;
		}

		private void Start() {
			Namespaces = new List<VoiceCommandNamespace>();

			GameObject.DontDestroyOnLoad(this);

			loadSettings();

			clientEndPoint = new IPEndPoint(IPAddress.Parse(HOST), CLIENT_PORT);
			serverEndPoint = new IPEndPoint(IPAddress.Parse(HOST), SERVER_PORT);
			client = new UdpClient(clientEndPoint);
			startReceive();

			Debug.Log("[VoiceCommander] now listening for commands from voice server");

			button = ToolbarManager.Instance.add("VoiceCommander", "VoiceCommander");
			button.ToolTip = "Toggle Voice Commander (Right-Click for Settings)";
			button.TexturePath = "blizzy/VoiceCommander/listening";
			button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.SPH, GameScenes.TRACKSTATION);
			button.OnClick += (e) => {
				if (e.MouseButton == 1) {
					toggleSettings();
				} else {
					toggleListen();
				}
			};

			GameEvents.onGameSceneLoadRequested.Add(sceneChanged);

			updateChecker = new UpdateChecker();
			updateChecker.OnDone += () => {
				UpdateAvailable = updateChecker.UpdateAvailable;
				updateChecker = null;
			};

			internalCommands.register();
			kspCommands.register();
		}

		private void startReceive() {
			client.BeginReceive(dataReceived, null);
		}

		private void OnDestroy() {
			saveSettings();

			internalCommands.unregister();
			kspCommands.unregister();

			client.Close();
			button.Destroy();
			Instance = null;
		}

		private void Update() {
			handleQueuedPackets();

			if (updateChecker != null) {
				updateChecker.update();
			}
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
					if ((listening && !isGamePaused()) || cmd.ExecuteAlways) {
						button.Drawable = new InfoDrawable(button, cmd.Label);
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

			if (!string.IsNullOrEmpty(yawText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_YAW_COMMAND, yawText));
			}
			if (!string.IsNullOrEmpty(pitchText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_PITCH_COMMAND, pitchText));
			}
			if (!string.IsNullOrEmpty(rollText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_ROLL_COMMAND, rollText));
			}
			if (!string.IsNullOrEmpty(progradeText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_PROGRADE_COMMAND, progradeText));
			}
			if (!string.IsNullOrEmpty(retrogradeText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_RETROGRADE_COMMAND, retrogradeText));
			}
			if (!string.IsNullOrEmpty(normalText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_NORMAL_COMMAND, normalText));
			}
			if (!string.IsNullOrEmpty(antiNormalText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_ANTI_NORMAL_COMMAND, antiNormalText));
			}
			if (!string.IsNullOrEmpty(radialText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_RADIAL_COMMAND, radialText));
			}
			if (!string.IsNullOrEmpty(antiRadialText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_ANTI_RADIAL_COMMAND, antiRadialText));
			}
			if (!string.IsNullOrEmpty(apoapsisText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_APOAPSIS_COMMAND, apoapsisText));
			}
			if (!string.IsNullOrEmpty(periapsisText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_PERIAPSIS_COMMAND, periapsisText));
			}
			if (!string.IsNullOrEmpty(maneuverNodeText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_MANEUVER_NODE_COMMAND, maneuverNodeText));
			}
			if (!string.IsNullOrEmpty(soiText)) {
				sendPacketToServer(new VoicePacket(PacketType.SET_SOI_COMMAND, soiText));
			}

			foreach (KeyValuePair<string, List<string>> cmdEntry in texts) {
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

		private void sendPacketToServer(VoicePacket packet) {
			byte[] data = packet.PacketData;
			client.Send(data, data.Length, serverEndPoint);
		}

		internal void toggleListen() {
			listening = !listening;
			button.TexturePath = listening ? "blizzy/VoiceCommander/listening" : "blizzy/VoiceCommander/idle";
		}

		private void sceneChanged(GameScenes scene) {
			settingsWindow = null;
		}

		private void toggleSettings() {
			if (settingsWindow != null) {
				settingsWindow = null;
			} else {
				Dictionary<VoiceCommand, string> dlgTexts = new Dictionary<VoiceCommand, string>();
				foreach (VoiceCommandNamespace ns in VoiceCommander.Instance.Namespaces) {
					foreach (VoiceCommand cmd in ns.Commands) {
						string fullCommandId = ns.Id + "/" + cmd.Id;
						string dlgText = string.Empty;
						if (texts.ContainsKey(fullCommandId)) {
							dlgText = string.Join("\n", texts[fullCommandId].ToArray());
						}
						dlgTexts.Add(cmd, dlgText);
					}
				}
				settingsWindow = new SettingsWindow(dlgTexts,
					yawText, pitchText, rollText,
					progradeText, retrogradeText, normalText, antiNormalText, radialText, antiRadialText,
					apoapsisText, periapsisText, maneuverNodeText, soiText,
					() => {
						yawText = settingsWindow.YawText;
						pitchText = settingsWindow.PitchText;
						rollText = settingsWindow.RollText;
						progradeText = settingsWindow.ProgradeText;
						retrogradeText = settingsWindow.RetrogradeText;
						normalText = settingsWindow.NormalText;
						antiNormalText = settingsWindow.AntiNormalText;
						radialText = settingsWindow.RadialText;
						antiRadialText = settingsWindow.AntiRadialText;
						apoapsisText = settingsWindow.ApoapsisText;
						periapsisText = settingsWindow.PeriapsisText;
						maneuverNodeText = settingsWindow.ManeuverNodeText;
						soiText = settingsWindow.SoIText;

						dlgTexts = settingsWindow.Texts;
						foreach (KeyValuePair<VoiceCommand, string> entry in dlgTexts) {
							VoiceCommand cmd = entry.Key;
							List<string> cmdTexts = new List<string>(entry.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
							VoiceCommandNamespace ns = Namespaces.FirstOrDefault(n => n.Commands.Contains(cmd));
							string fullCommandId = ns.Id + "/" + cmd.Id;
							texts[fullCommandId] = cmdTexts;
						}

						settingsWindow = null;

						saveSettings();
						updateCommandsOnServer();
					}, () => {
						settingsWindow = null;
					});
			}
		}

		private void loadSettings() {
			ConfigNode rootNode = ConfigNode.Load(SETTINGS_FILE) ?? new ConfigNode();

			ConfigNode textsNode = rootNode.getOrCreateNode("texts");
			yawText = textsNode.get("yaw", (string) null);
			pitchText = textsNode.get("pitch", (string) null);
			rollText = textsNode.get("roll", (string) null);
			progradeText = textsNode.get("prograde", (string) null);
			retrogradeText = textsNode.get("retrograde", (string) null);
			normalText = textsNode.get("normal", (string) null);
			antiNormalText = textsNode.get("antiNormal", (string) null);
			radialText = textsNode.get("radial", (string) null);
			antiRadialText = textsNode.get("antiRadial", (string) null);
			apoapsisText = textsNode.get("apoapsis", (string) null);
			periapsisText = textsNode.get("periapsis", (string) null);
			maneuverNodeText = textsNode.get("maneuverNode", (string) null);
			soiText = textsNode.get("SoI", (string) null);

			texts = new Dictionary<string, List<string>>();
			foreach (ConfigNode nsNode in textsNode.nodes) {
				string ns = nsNode.name;
				foreach (ConfigNode.Value value in nsNode.values) {
					string cmd = value.name;
					string text = value.value;

					List<string> cmdTexts;
					string fullCommandId = ns + "/" + cmd;
					if (texts.ContainsKey(fullCommandId)) {
						cmdTexts = texts[fullCommandId];
					} else {
						cmdTexts = new List<string>();
						texts.Add(fullCommandId, cmdTexts);
					}
					cmdTexts.Add(text);
				}
			}
		}

		private void saveSettings() {
			ConfigNode rootNode = new ConfigNode();
			ConfigNode textsNode = rootNode.getOrCreateNode("texts");

			if (!string.IsNullOrEmpty(yawText)) {
				textsNode.overwrite("yaw", yawText);
			}
			if (!string.IsNullOrEmpty(pitchText)) {
				textsNode.overwrite("pitch", pitchText);
			}
			if (!string.IsNullOrEmpty(rollText)) {
				textsNode.overwrite("roll", rollText);
			}
			if (!string.IsNullOrEmpty(progradeText)) {
				textsNode.overwrite("prograde", progradeText);
			}
			if (!string.IsNullOrEmpty(retrogradeText)) {
				textsNode.overwrite("retrograde", retrogradeText);
			}
			if (!string.IsNullOrEmpty(normalText)) {
				textsNode.overwrite("normal", normalText);
			}
			if (!string.IsNullOrEmpty(antiNormalText)) {
				textsNode.overwrite("antiNormal", antiNormalText);
			}
			if (!string.IsNullOrEmpty(radialText)) {
				textsNode.overwrite("radial", radialText);
			}
			if (!string.IsNullOrEmpty(antiRadialText)) {
				textsNode.overwrite("antiRadial", antiRadialText);
			}
			if (!string.IsNullOrEmpty(apoapsisText)) {
				textsNode.overwrite("apoapsis", apoapsisText);
			}
			if (!string.IsNullOrEmpty(periapsisText)) {
				textsNode.overwrite("periapsis", periapsisText);
			}
			if (!string.IsNullOrEmpty(maneuverNodeText)) {
				textsNode.overwrite("maneuverNode", maneuverNodeText);
			}
			if (!string.IsNullOrEmpty(soiText)) {
				textsNode.overwrite("SoI", soiText);
			}

			foreach (KeyValuePair<string, List<string>> cmdEntry in texts) {
				string[] parts = cmdEntry.Key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				string ns = parts[0];
				string cmd = parts[1];
				ConfigNode nsNode = textsNode.getOrCreateNode(ns);
				foreach (string text in cmdEntry.Value) {
					if (text != string.Empty) {
						nsNode.AddValue(cmd, text);
					}
				}
			}

			rootNode.Save(SETTINGS_FILE);
		}
	}
}
