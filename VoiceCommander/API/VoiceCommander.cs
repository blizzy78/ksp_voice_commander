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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace VoiceCommander {
	public partial class VoiceCommander : MonoBehaviour {
		public static VoiceCommander Instance;

		public Vessel[] Vessels {
			get;
			// KSPCommands sets this for <vesselName>
			internal set;
		}

		public void AddNamespace(VoiceCommandNamespace ns) {
			Namespaces.Add(ns);
			updateCommandsOnServer();
		}

		public void RemoveNamespace(VoiceCommandNamespace ns) {
			Namespaces.Remove(ns);
			string fullMacroIdPrefix = ns.Id + "/";
			foreach (string fullMacroId in new List<string>(macroValueTexts.Keys.Where(k => k.StartsWith(fullMacroIdPrefix)))) {
				macroValueTexts.Remove(fullMacroId);
			}
			updateCommandsOnServer();
		}

		public void SetMacroValueTexts(VoiceCommandNamespace ns, string macroId, string[] texts) {
			string fullMacroId = ns.Id + "/" + macroId;
			if (macroValueTexts.ContainsKey(fullMacroId)) {
				macroValueTexts[fullMacroId] = texts;
			} else {
				macroValueTexts.Add(fullMacroId, texts);
			}
			updateCommandsOnServer();
		}
	}
}
