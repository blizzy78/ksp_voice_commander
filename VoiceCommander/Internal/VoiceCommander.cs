﻿/*
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
		internal const int VERSION = 1;

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
		private VoiceCommandNamespace internalNamespace;
		private VoiceCommandNamespace kspNamespace;
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

			addInternalNamespace();
			addKSPNamespace();
		}

		private void addInternalNamespace() {
			internalNamespace = new VoiceCommandNamespace("voiceCommander", "Voice Commander");
			VoiceCommand toggleListenCmd = new VoiceCommand("toggleListen", "Toggle Listening", toggleListen);
			toggleListenCmd.ExecuteAlways = true;
			internalNamespace.AddCommand(toggleListenCmd);
			AddNamespace(internalNamespace);
		}

		private void addKSPNamespace() {
			kspNamespace = new VoiceCommandNamespace("ksp", "KSP");
			kspNamespace.AddCommand(new VoiceCommand("quicksave", "Quick Save", kspQuickSave));
			kspNamespace.AddCommand(new VoiceCommand("quickload", "Quick Load", kspQuickLoad));
			kspNamespace.AddCommand(new VoiceCommand("toggleMap", "Toggle Map View", kspToggleMap));
			kspNamespace.AddCommand(new VoiceCommand("cameraFreeMode", "Set Camera to Free Mode", () => kspCameraMode(FlightCamera.Modes.FREE)));
			kspNamespace.AddCommand(new VoiceCommand("cameraChaseMode", "Set Camera to Chase Mode", () => kspCameraMode(FlightCamera.Modes.CHASE)));
			kspNamespace.AddCommand(new VoiceCommand("stage", "Activate Next Stage", kspStage));
			kspNamespace.AddCommand(new VoiceCommand("throttleFull", "Set Throttle to Full", () => kspThrottle(1f)));
			kspNamespace.AddCommand(new VoiceCommand("throttleZero", "Set Throttle to Zero", () => kspThrottle(0f)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup1", "Activate Action Group 1", () => kspActionGroup(KSPActionGroup.Custom01)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup2", "Activate Action Group 2", () => kspActionGroup(KSPActionGroup.Custom02)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup3", "Activate Action Group 3", () => kspActionGroup(KSPActionGroup.Custom03)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup4", "Activate Action Group 4", () => kspActionGroup(KSPActionGroup.Custom04)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup5", "Activate Action Group 5", () => kspActionGroup(KSPActionGroup.Custom05)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup6", "Activate Action Group 6", () => kspActionGroup(KSPActionGroup.Custom06)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup7", "Activate Action Group 7", () => kspActionGroup(KSPActionGroup.Custom07)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup8", "Activate Action Group 8", () => kspActionGroup(KSPActionGroup.Custom08)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup9", "Activate Action Group 9", () => kspActionGroup(KSPActionGroup.Custom09)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroup10", "Activate Action Group 10", () => kspActionGroup(KSPActionGroup.Custom10)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroupGear", "Activate Action Group 'Gear'", () => kspActionGroup(KSPActionGroup.Gear)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroupBrakes", "Activate Action Group 'Brakes'", () => kspActionGroup(KSPActionGroup.Brakes)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroupLight", "Activate Action Group 'Light'", () => kspActionGroup(KSPActionGroup.Light)));
			kspNamespace.AddCommand(new VoiceCommand("actionGroupAbort", "Activate Action Group 'Abort'", () => kspActionGroup(KSPActionGroup.Abort)));
			AddNamespace(kspNamespace);
		}

		private void startReceive() {
			client.BeginReceive(dataReceived, null);
		}

		private void OnDestroy() {
			RemoveNamespace(kspNamespace);
			RemoveNamespace(internalNamespace);

			saveSettings();

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
			string dataStr = packet.Data;
			int pos = dataStr.IndexOf('|');
			string confidenceStr = dataStr.Substring(0, pos);
			float confidence = float.Parse(confidenceStr, CultureInfo.InvariantCulture);
			string text = dataStr.Substring(pos + 1);
			handleSpeechRecognized(text, confidence);
		}

		private void handleSpeechRecognized(string text, float confidence) {
			Debug.Log(string.Format("[VoiceCommander] command received: {0} (confidence: {1})", text, confidence));
			if (confidence >= MIN_CONFIDENCE) {
				VoiceCommand cmd = findCommand(text);
				if (cmd != null) {
					if (listening || cmd.ExecuteAlways) {
						try {
							cmd.Callback();
						} catch (Exception e) {
							VoiceCommandNamespace ns = Namespaces.FirstOrDefault(n => n.Commands.Contains(cmd));
							Debug.LogError(string.Format("[VoiceCommander] error while executing command: {0}/{1}", ns.Id, cmd.Id));
							Debug.LogException(e);
						}
					} else {
						Debug.Log(string.Format("[VoiceCommander] not listening, ignoring command: {0}", text));
					}
				} else {
					Debug.Log(string.Format("[VoiceCommander] unknown command received: {0}", text));
				}
			}
		}

		private VoiceCommand findCommand(string text) {
			foreach (KeyValuePair<string, List<string>> cmdEntry in texts) {
				if (cmdEntry.Value.Contains(text)) {
					string[] parts = cmdEntry.Key.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					string nsId = parts[0];
					string cmdId = parts[1];
					return Namespaces.FirstOrDefault(n => n.Id == nsId).Commands.FirstOrDefault(c => c.Id == cmdId);
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

			foreach (KeyValuePair<string, List<string>> cmdEntry in texts) {
				foreach (string text in cmdEntry.Value) {
					sendPacketToServer(new VoicePacket(PacketType.ADD_COMMAND, text));
				}
			}
		}

		private void sendPacketToServer(VoicePacket packet) {
			byte[] data = packet.PacketData;
			client.Send(data, data.Length, serverEndPoint);
		}

		private void toggleListen() {
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
						string fullCommandId = ns.Id + "|" + cmd.Id;
						string dlgText = string.Empty;
						if (texts.ContainsKey(fullCommandId)) {
							dlgText = string.Join("\n", texts[fullCommandId].ToArray());
						}
						dlgTexts.Add(cmd, dlgText);
					}
				}
				settingsWindow = new SettingsWindow(dlgTexts, () => {
					dlgTexts = settingsWindow.Texts;
					foreach (KeyValuePair<VoiceCommand, string> entry in dlgTexts) {
						VoiceCommand cmd = entry.Key;
						List<string> cmdTexts = new List<string>(entry.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
						VoiceCommandNamespace ns = Namespaces.FirstOrDefault(n => n.Commands.Contains(cmd));
						string fullCommandId = ns.Id + "|" + cmd.Id;
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

			texts = new Dictionary<string, List<string>>();
			ConfigNode textsNode = rootNode.getOrCreateNode("texts");
			foreach (ConfigNode nsNode in textsNode.nodes) {
				string ns = nsNode.name;
				foreach (ConfigNode.Value value in nsNode.values) {
					string cmd = value.name;
					string text = value.value;

					List<string> cmdTexts;
					string fullCommandId = ns + "|" + cmd;
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
			foreach (KeyValuePair<string, List<string>> cmdEntry in texts) {
				string[] parts = cmdEntry.Key.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				string ns = parts[0];
				string cmd = parts[1];
				ConfigNode nsNode = textsNode.getOrCreateNode(ns);
				foreach (string text in cmdEntry.Value) {
					nsNode.AddValue(cmd, text);
				}
			}

			rootNode.Save(SETTINGS_FILE);
		}

		private void kspQuickSave() {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				QuickSaveLoad.QuickSave();
			}
		}

		private void kspQuickLoad() {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				HighLogic.CurrentGame = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
				HighLogic.CurrentGame.startScene = GameScenes.FLIGHT;
				HighLogic.CurrentGame.Start();
			}
		}

		private void kspToggleMap() {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				if (MapView.MapIsEnabled) {
					MapView.ExitMapView();
				} else {
					MapView.EnterMapView();
				}
			}
		}

		private void kspCameraMode(FlightCamera.Modes mode) {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				FlightCamera.fetch.setMode(mode);
			}
		}

		private void kspStage() {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				Staging.ActivateNextStage();
			}
		}

		private void kspThrottle(float throttle) {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				FlightInputHandler.state.mainThrottle = throttle;
			}
		}

		private void kspActionGroup(KSPActionGroup group) {
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(group);
			}
		}
	}
}