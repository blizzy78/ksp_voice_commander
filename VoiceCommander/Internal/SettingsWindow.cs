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
using System.Linq;
using System.Text;
using UnityEngine;

namespace VoiceCommander {
	internal class SettingsWindow {
		private const string COMMANDS_DOC_URL = "http://bit.ly/1lnCdEv";
		private const long SET_KEY_WAIT_TIME = 5000;

		internal Dictionary<VoiceCommand, string> Texts {
			get;
			private set;
		}

		internal string YawText {
			get;
			private set;
		}
		internal string PitchText {
			get;
			private set;
		}
		internal string RollText {
			get;
			private set;
		}
		internal string ProgradeText {
			get;
			private set;
		}
		internal string RetrogradeText {
			get;
			private set;
		}
		internal string NormalText {
			get;
			private set;
		}
		internal string AntiNormalText {
			get;
			private set;
		}
		internal string RadialText {
			get;
			private set;
		}
		internal string AntiRadialText {
			get;
			private set;
		}
		internal string ApoapsisText {
			get;
			private set;
		}
		internal string PeriapsisText {
			get;
			private set;
		}
		internal string ManeuverNodeText {
			get;
			private set;
		}
		internal string SoIText {
			get;
			private set;
		}

		internal bool PushToTalk {
			get;
			private set;
		}
		internal KeyCode PushToTalkKey {
			get;
			private set;
		}

		private Settings settings;
		private Action onOk;
		private Action onCancel;
		private int id = UnityEngine.Random.Range(0, int.MaxValue);
		private Rect rect = new Rect(100, 100, 0, 0);
		private Vector2 scrollPos = new Vector2(0, 0);
		private bool waitingForPushToTalkKey;
		private long waitForPushToTalkKeyStartTime;
		private GUIStyle rightAlignedLabelStyle;
		private bool stylesInitialized;

		internal SettingsWindow(Settings settings, Action onOk, Action onCancel) {
			this.settings = settings;

			Texts = new Dictionary<VoiceCommand, string>();
			foreach (VoiceCommandNamespace ns in VoiceCommander.Instance.Namespaces) {
				foreach (VoiceCommand cmd in ns.Commands) {
					string fullCommandId = ns.Id + "/" + cmd.Id;
					string dlgText = string.Empty;
					if (settings.texts.ContainsKey(fullCommandId)) {
						dlgText = string.Join("\n", settings.texts[fullCommandId].ToArray());
					}
					Texts.Add(cmd, dlgText);
				}
			}

			this.YawText = settings.yawText;
			this.PitchText = settings.pitchText;
			this.RollText = settings.rollText;
			this.ProgradeText = settings.progradeText;
			this.RetrogradeText = settings.retrogradeText;
			this.NormalText = settings.normalText;
			this.AntiNormalText = settings.antiNormalText;
			this.RadialText = settings.radialText;
			this.AntiRadialText = settings.antiRadialText;
			this.ApoapsisText = settings.apoapsisText;
			this.PeriapsisText = settings.periapsisText;
			this.ManeuverNodeText = settings.maneuverNodeText;
			this.SoIText = settings.soiText;

			this.PushToTalk = settings.pushToTalk;
			this.PushToTalkKey = settings.pushToTalkKey;

			this.onOk = onOk;
			this.onCancel = onCancel;
		}

		internal void saveToSettings() {
			settings.yawText = YawText;
			settings.pitchText = PitchText;
			settings.rollText = RollText;
			settings.progradeText = ProgradeText;
			settings.retrogradeText = RetrogradeText;
			settings.normalText = NormalText;
			settings.antiNormalText = AntiNormalText;
			settings.radialText = RadialText;
			settings.antiRadialText = AntiRadialText;
			settings.apoapsisText = ApoapsisText;
			settings.periapsisText = PeriapsisText;
			settings.maneuverNodeText = ManeuverNodeText;
			settings.soiText = SoIText;

			settings.pushToTalk = PushToTalk;
			settings.pushToTalkKey = PushToTalkKey;

			foreach (KeyValuePair<VoiceCommand, string> entry in Texts) {
				VoiceCommand cmd = entry.Key;
				List<string> cmdTexts = new List<string>(entry.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
				VoiceCommandNamespace ns = VoiceCommander.Instance.Namespaces.FirstOrDefault(n => n.Commands.Contains(cmd));
				string fullCommandId = ns.Id + "/" + cmd.Id;
				settings.texts[fullCommandId] = cmdTexts;
			}
		}

		internal void draw() {
			initStyles();

			rect = GUILayout.Window(id, rect, (windowId) => drawContents(), "Voice Commander Settings");
		}

		private void drawContents() {
			GUILayout.BeginVertical();

			scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(450), GUILayout.Height(350));

			GUILayout.BeginVertical();
			drawMacroTexts();
			GUILayout.Space(10);
			drawCommandTexts();
			GUILayout.EndVertical();

			GUILayout.EndScrollView();

			GUILayout.Space(15);

			drawPushToTalk();

			if (VoiceCommander.Instance.UpdateAvailable) {
				GUILayout.Space(15);
				Color oldColor = GUI.color;
				GUI.color = Color.yellow;
				GUILayout.Label("An update of the Voice Commander plugin is available.");
				GUI.color = oldColor;
			}

			GUILayout.Space(15);

			drawButtons();

			GUILayout.EndVertical();

			GUI.DragWindow();
		}

		private void drawMacroTexts() {
			GUILayout.Label("Specify the texts to speak for general words here. These are used in macros.");

			YawText = drawLabelAndText("Yaw", YawText);
			PitchText = drawLabelAndText("Pitch", PitchText);
			RollText = drawLabelAndText("Roll", RollText);
			ProgradeText = drawLabelAndText("Prograde", ProgradeText);
			RetrogradeText = drawLabelAndText("Retrograde", RetrogradeText);
			NormalText = drawLabelAndText("Normal", NormalText);
			AntiNormalText = drawLabelAndText("Anti-normal", AntiNormalText);
			RadialText = drawLabelAndText("Radial", RadialText);
			AntiRadialText = drawLabelAndText("Anti-radial", AntiRadialText);
			ApoapsisText = drawLabelAndText("Apoapsis", ApoapsisText);
			PeriapsisText = drawLabelAndText("Periapsis", PeriapsisText);
			ManeuverNodeText = drawLabelAndText("Maneuver node", ManeuverNodeText);
			SoIText = drawLabelAndText("Sphere of influence", SoIText);
		}

		private string drawLabelAndText(string label, string text) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(label + ":", rightAlignedLabelStyle, GUILayout.Width(130));
			bool oldEnabled = GUI.enabled;
			GUI.enabled = !waitingForPushToTalkKey;
			text = GUILayout.TextField(text ?? string.Empty);
			GUI.enabled = oldEnabled;
			GUILayout.EndHorizontal();
			return text;
		}

		private void drawCommandTexts() {
			GUILayout.Label("For each command, you can configure the necessary text to speak here. You may specify multiple texts for each command, each on a single line.");
			GUILayout.Label("Note: Some of the commands may not be applicable to every game scene.");

			foreach (VoiceCommandNamespace ns in VoiceCommander.Instance.Namespaces) {
				foreach (VoiceCommand cmd in ns.Commands) {
					GUILayout.Label(string.Format("{0}: {1}", ns.Label, cmd.Label));
					bool oldEnabled = GUI.enabled;
					GUI.enabled = !waitingForPushToTalkKey;
					Texts[cmd] = GUILayout.TextArea(Texts[cmd], GUILayout.Height(45));
					GUI.enabled = oldEnabled;
				}
			}
		}

		private void drawPushToTalk() {
			GUILayout.BeginHorizontal();

			PushToTalk = GUILayout.Toggle(PushToTalk,
				string.Format("Push key to talk: {0}", waitingForPushToTalkKey ? "Press key now..." : PushToTalkKey.ToString()));

			bool oldEnabled = GUI.enabled;
			GUI.enabled = !waitingForPushToTalkKey && PushToTalk;
			if (GUILayout.Button("Set")) {
				waitingForPushToTalkKey = true;
				waitForPushToTalkKeyStartTime = DateTime.UtcNow.Ticks / 10000;
			}
			GUI.enabled = !waitingForPushToTalkKey && (PushToTalkKey != KeyCode.None);
			if (GUILayout.Button("Clear")) {
				PushToTalkKey = KeyCode.None;
			}
			GUI.enabled = oldEnabled;

			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();
		}

		private void drawButtons() {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Open Documentation")) {
				Application.OpenURL(COMMANDS_DOC_URL);
			}
			bool oldEnabled = GUI.enabled;
			GUI.enabled = !waitingForPushToTalkKey;
			if (GUILayout.Button("OK")) {
				onOk();
			}
			if (GUILayout.Button("Cancel")) {
				onCancel();
			}
			GUI.enabled = oldEnabled;
			GUILayout.EndHorizontal();
		}

		private void initStyles() {
			if (!stylesInitialized) {
				rightAlignedLabelStyle = new GUIStyle(GUI.skin.label);
				rightAlignedLabelStyle.alignment = TextAnchor.MiddleRight;

				stylesInitialized = true;
			}
		}

		internal void update() {
			if (waitingForPushToTalkKey) {
				long now = DateTime.UtcNow.Ticks / 10000;
				if ((now - waitForPushToTalkKeyStartTime) < SET_KEY_WAIT_TIME) {
					if (Input.anyKeyDown && (GUIUtility.keyboardControl == 0)) {
						KeyCode keyCode = Utils.getCurrentInputKeyDown();
						if (keyCode != KeyCode.None) {
							PushToTalkKey = keyCode;
						}
						waitingForPushToTalkKey = false;
					}
				} else {
					waitingForPushToTalkKey = false;
				}
			}
		}
	}
}
