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

namespace VoiceServer {
	internal class Texts {
		internal bool HaveAxisTexts {
			get {
				lock (this) {
					return !string.IsNullOrEmpty(yawText) && !string.IsNullOrEmpty(pitchText) && !string.IsNullOrEmpty(rollText);
				}
			}
		}

		internal bool HaveFlightDirectionTexts {
			get {
				lock (this) {
					return !string.IsNullOrEmpty(progradeText) && !string.IsNullOrEmpty(retrogradeText) && !string.IsNullOrEmpty(normalText) &&
						!string.IsNullOrEmpty(antiNormalText) && !string.IsNullOrEmpty(radialText) && !string.IsNullOrEmpty(antiRadialText) &&
						!string.IsNullOrEmpty(maneuverNodeText);
				}
			}
		}

		internal bool HaveApPeTexts {
			get {
				lock (this) {
					return !string.IsNullOrEmpty(apoapsisText) && !string.IsNullOrEmpty(periapsisText);
				}
			}
		}

		internal bool HaveWarpTargetTexts {
			get {
				lock (this) {
					return !string.IsNullOrEmpty(apoapsisText) && !string.IsNullOrEmpty(periapsisText) &&
						!string.IsNullOrEmpty(maneuverNodeText) && !string.IsNullOrEmpty(soiText);
				}
			}
		}

		internal Dictionary<string, List<string>> commands = new Dictionary<string, List<string>>();
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
		internal Dictionary<string, List<string>> macroValueTexts = new Dictionary<string, List<string>>();

		internal void clear() {
			commands.Clear();
			yawText = null;
			pitchText = null;
			rollText = null;
			progradeText = null;
			retrogradeText = null;
			normalText = null;
			antiNormalText = null;
			radialText = null;
			antiRadialText = null;
			apoapsisText = null;
			periapsisText = null;
			maneuverNodeText = null;
			soiText = null;
			macroValueTexts.Clear();
		}
	}
}
