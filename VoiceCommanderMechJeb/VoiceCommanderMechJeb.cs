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
using VoiceCommander;
using MuMech;

namespace VoiceCommanderMechJeb {
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class VoiceCommanderMechJeb : MonoBehaviour {
		private VoiceCommandNamespace ns;

		private void Start() {
			Debug.Log("[VoiceCommanderMechJeb] registering commands");

			ns = new VoiceCommandNamespace("mechJeb", "MechJeb");
			ns.AddCommand(new VoiceCommand("turnPrograde", "Turn Prograde", () => attitude(new Vector3d(0, 0, 1))));
			ns.AddCommand(new VoiceCommand("turnRetrograde", "Turn Retrograde", () => attitude(new Vector3d(0, 0, -1))));
			ns.AddCommand(new VoiceCommand("turnNormal", "Turn Normal", () => attitude(new Vector3d(1, 0, 0))));
			ns.AddCommand(new VoiceCommand("turnAntinormal", "Turn Anti-Normal", () => attitude(new Vector3d(-1, 0, 0))));
			ns.AddCommand(new VoiceCommand("turnRadial", "Turn Radial", () => attitude(new Vector3d(0, 1, 0))));
			ns.AddCommand(new VoiceCommand("turnAntiradial", "Turn Anti-Radial", () => attitude(new Vector3d(0, -1, 0))));
			ns.AddCommand(new VoiceCommand("killRotation", "Kill Rotation", killRotation));

			VoiceCommander.VoiceCommander.Instance.AddNamespace(ns);
		}

		private void OnDestroy() {
			Debug.Log("[VoiceCommanderMechJeb] unregistering commands");
			VoiceCommander.VoiceCommander.Instance.RemoveNamespace(ns);
		}

		private void attitude(Vector3d direction) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.attitudeTo(direction, AttitudeReference.ORBIT, this);
			}
		}

		private void killRotation() {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.users.Remove(this);
				FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
			}
		}

		private MechJebCore getMechJeb() {
			return FlightGlobals.ActiveVessel.FindPartModulesImplementing<MechJebCore>().FirstOrDefault();
		}
	}
}
