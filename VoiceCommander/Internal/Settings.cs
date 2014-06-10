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
using System.Linq;
using System.Text;
using UnityEngine;

namespace VoiceCommander {
	internal class Settings {
		private static readonly string SETTINGS_FILE = KSPUtil.ApplicationRootPath + "GameData/blizzy/VoiceCommander/settings.dat";

		internal Dictionary<string, List<string>> texts;
		internal string yawText;
		internal string pitchText;
		internal string rollText;
		internal string progradeText;
		internal string retrogradeText;
		internal string normalText;
		internal string antiNormalText;
		internal string radialText;
		internal string antiRadialText;
		internal string apoapsisText;
		internal string periapsisText;
		internal string maneuverNodeText;
		internal string soiText;
		internal bool pushToTalk;
		internal KeyCode pushToTalkKey = KeyCode.None;

		private Settings() {
			// nothing to do
		}

		internal static Settings load() {
			Settings settings = new Settings();

			ConfigNode rootNode = ConfigNode.Load(SETTINGS_FILE) ?? new ConfigNode();

			settings.pushToTalk = rootNode.get("pushToTalk", false);
			string pushToTalkKeyStr = rootNode.get("pushToTalkKey", KeyCode.None.ToString());
			settings.pushToTalkKey = (KeyCode) Enum.Parse(typeof(KeyCode), pushToTalkKeyStr);

			ConfigNode textsNode = rootNode.getOrCreateNode("texts");
			settings.yawText = textsNode.get("yaw", (string) null);
			settings.pitchText = textsNode.get("pitch", (string) null);
			settings.rollText = textsNode.get("roll", (string) null);
			settings.progradeText = textsNode.get("prograde", (string) null);
			settings.retrogradeText = textsNode.get("retrograde", (string) null);
			settings.normalText = textsNode.get("normal", (string) null);
			settings.antiNormalText = textsNode.get("antiNormal", (string) null);
			settings.radialText = textsNode.get("radial", (string) null);
			settings.antiRadialText = textsNode.get("antiRadial", (string) null);
			settings.apoapsisText = textsNode.get("apoapsis", (string) null);
			settings.periapsisText = textsNode.get("periapsis", (string) null);
			settings.maneuverNodeText = textsNode.get("maneuverNode", (string) null);
			settings.soiText = textsNode.get("SoI", (string) null);

			settings.texts = new Dictionary<string, List<string>>();
			foreach (ConfigNode nsNode in textsNode.nodes) {
				string ns = nsNode.name;
				foreach (ConfigNode.Value value in nsNode.values) {
					string cmd = value.name;
					string text = value.value;

					List<string> cmdTexts;
					string fullCommandId = ns + "/" + cmd;
					if (settings.texts.ContainsKey(fullCommandId)) {
						cmdTexts = settings.texts[fullCommandId];
					} else {
						cmdTexts = new List<string>();
						settings.texts.Add(fullCommandId, cmdTexts);
					}
					cmdTexts.Add(text);
				}
			}

			return settings;
		}

		internal void save() {
			ConfigNode rootNode = new ConfigNode();

			rootNode.overwrite("pushToTalk", pushToTalk);
			rootNode.overwrite("pushToTalkKey", pushToTalkKey.ToString());

			ConfigNode textsNode = rootNode.getOrCreateNode("texts");
			textsNode.overwrite("yaw", yawText);
			textsNode.overwrite("pitch", pitchText);
			textsNode.overwrite("roll", rollText);
			textsNode.overwrite("prograde", progradeText);
			textsNode.overwrite("retrograde", retrogradeText);
			textsNode.overwrite("normal", normalText);
			textsNode.overwrite("antiNormal", antiNormalText);
			textsNode.overwrite("radial", radialText);
			textsNode.overwrite("antiRadial", antiRadialText);
			textsNode.overwrite("apoapsis", apoapsisText);
			textsNode.overwrite("periapsis", periapsisText);
			textsNode.overwrite("maneuverNode", maneuverNodeText);
			textsNode.overwrite("SoI", soiText);

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
