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
		internal Dictionary<VoiceCommand, string> Texts {
			get;
			private set;
		}

		private Action onOk;
		private Action onCancel;
		private int id = UnityEngine.Random.Range(0, int.MaxValue);
		private Rect rect = new Rect(100, 100, 0, 0);
		private Vector2 scrollPos = new Vector2(0, 0);

		internal SettingsWindow(Dictionary<VoiceCommand, string> texts, Action onOk, Action onCancel) {
			this.Texts = texts;
			this.onOk = onOk;
			this.onCancel = onCancel;
		}

		internal void draw() {
			rect = GUILayout.Window(id, rect, (windowId) => drawContents(), "Voice Commander Settings");
		}

		private void drawContents() {
			GUILayout.BeginVertical();

			GUILayout.Label("For each command, you can configure the necessary text to speak here. You may specify multiple texts for each command, each on a single line.");
			GUILayout.Label("Note: Some of the commands may not be applicable to every game scene.");

			scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(400), GUILayout.Height(400));

			GUILayout.BeginVertical();

			foreach (VoiceCommandNamespace ns in VoiceCommander.Instance.Namespaces) {
				foreach (VoiceCommand cmd in ns.Commands) {
					GUILayout.Label(string.Format("{0}: {1}", ns.Label, cmd.Label));
					Texts[cmd] = GUILayout.TextArea(Texts[cmd], GUILayout.Height(45));
				}
			}

			GUILayout.EndVertical();

			GUILayout.EndScrollView();

			if (VoiceCommander.Instance.UpdateAvailable) {
				GUILayout.Space(5);
				Color oldColor = GUI.color;
				GUI.color = Color.yellow;
				GUILayout.Label("An update of this plugin is available.");
				GUI.color = oldColor;
			}

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("OK")) {
				onOk();
			}
			if (GUILayout.Button("Cancel")) {
				onCancel();
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();

			GUI.DragWindow();
		}
	}
}
