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

		private Action onOk;
		private Action onCancel;
		private int id = UnityEngine.Random.Range(0, int.MaxValue);
		private Rect rect = new Rect(100, 100, 0, 0);
		private Vector2 scrollPos = new Vector2(0, 0);

		internal SettingsWindow(Dictionary<VoiceCommand, string> texts,
			string yawText, string pitchText, string rollText,
			string progradeText, string retrogradeText, string normalText, string antiNormalText, string radialText, string antiRadialText,
			string apoapsisText, string periapsisText,
			Action onOk, Action onCancel) {

			this.Texts = texts;

			this.YawText = yawText;
			this.PitchText = pitchText;
			this.RollText = rollText;
			this.ProgradeText = progradeText;
			this.RetrogradeText = retrogradeText;
			this.NormalText = normalText;
			this.AntiNormalText = antiNormalText;
			this.RadialText = radialText;
			this.AntiRadialText = antiRadialText;
			this.ApoapsisText = apoapsisText;
			this.PeriapsisText = periapsisText;

			this.onOk = onOk;
			this.onCancel = onCancel;
		}

		internal void draw() {
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

			if (VoiceCommander.Instance.UpdateAvailable) {
				GUILayout.Space(10);
				Color oldColor = GUI.color;
				GUI.color = Color.yellow;
				GUILayout.Label("An update of the Voice Commander plugin is available.");
				GUI.color = oldColor;
			}

			GUILayout.Space(10);

			drawButtons();

			GUILayout.EndVertical();

			GUI.DragWindow();
		}

		private void drawMacroTexts() {
			GUILayout.Label("Specify the texts to speak for general words here. These are used in macros.");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Yaw:", GUILayout.Width(100));
			YawText = GUILayout.TextField(YawText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Pitch:", GUILayout.Width(100));
			PitchText = GUILayout.TextField(PitchText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Roll:", GUILayout.Width(100));
			RollText = GUILayout.TextField(RollText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Prograde:", GUILayout.Width(100));
			ProgradeText = GUILayout.TextField(ProgradeText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Retrograde:", GUILayout.Width(100));
			RetrogradeText = GUILayout.TextField(RetrogradeText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Normal:", GUILayout.Width(100));
			NormalText = GUILayout.TextField(NormalText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Anti-normal:", GUILayout.Width(100));
			AntiNormalText = GUILayout.TextField(AntiNormalText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Radial:", GUILayout.Width(100));
			RadialText = GUILayout.TextField(RadialText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Anti-radial:", GUILayout.Width(100));
			AntiRadialText = GUILayout.TextField(AntiRadialText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Apoapsis:", GUILayout.Width(100));
			ApoapsisText = GUILayout.TextField(ApoapsisText ?? string.Empty);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Periapsis:", GUILayout.Width(100));
			PeriapsisText = GUILayout.TextField(PeriapsisText ?? string.Empty);
			GUILayout.EndHorizontal();
		}

		private void drawCommandTexts() {
			GUILayout.Label("For each command, you can configure the necessary text to speak here. You may specify multiple texts for each command, each on a single line.");
			GUILayout.Label("Note: Some of the commands may not be applicable to every game scene.");

			foreach (VoiceCommandNamespace ns in VoiceCommander.Instance.Namespaces) {
				foreach (VoiceCommand cmd in ns.Commands) {
					GUILayout.Label(string.Format("{0}: {1}", ns.Label, cmd.Label));
					Texts[cmd] = GUILayout.TextArea(Texts[cmd], GUILayout.Height(45));
				}
			}
		}

		private void drawButtons() {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Open Documentation")) {
				Application.OpenURL(COMMANDS_DOC_URL);
			}
			if (GUILayout.Button("OK")) {
				onOk();
			}
			if (GUILayout.Button("Cancel")) {
				onCancel();
			}
			GUILayout.EndHorizontal();
		}
	}
}
