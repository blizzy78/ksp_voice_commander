﻿/*
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VoiceCommander {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class InstallChecker : MonoBehaviour {
		internal void Start() {
			string executingAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			IEnumerable<AssemblyLoader.LoadedAssembly> assemblies = AssemblyLoader.loadedAssemblies
					.Where(a => (a.assembly.GetName().Name == executingAssemblyName) && (a.url != "blizzy/VoiceCommander"));
			if (assemblies.Any()) {
				Uri rootUri = new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath));
				IEnumerable<string> badPaths = assemblies
					.Select(a => Uri.UnescapeDataString(rootUri.MakeRelativeUri(new Uri(a.path)).ToString().Replace('/', Path.DirectorySeparatorChar)));
				PopupDialog.SpawnPopupDialog("Incorrect Voice Commander Installation",
					"The Voice Commander plugin has been installed incorrectly and will not function properly. All Voice Commander files " +
					"should be located in GameData" + Path.DirectorySeparatorChar + "blizzy" + Path.DirectorySeparatorChar + "VoiceCommander. Do not move any files from " +
					"inside the Voice Commander folder.\n\n" +
					"Incorrect path(s):\n\n" + string.Join("\n", badPaths.ToArray()),
					"OK", false, HighLogic.Skin);
			}
		}
	}
}
