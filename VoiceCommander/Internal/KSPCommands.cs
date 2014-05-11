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

namespace VoiceCommander {
	internal class KSPCommands {
		internal VoiceCommandNamespace Namespace {
			get;
			private set;
		}

		internal KSPCommands() {
			Namespace = new VoiceCommandNamespace("ksp", "KSP");
			Namespace.AddCommand(new VoiceCommand("quicksave", "Quick Save", quickSave));
			Namespace.AddCommand(new VoiceCommand("quickload", "Quick Load", quickLoad));
			Namespace.AddCommand(new VoiceCommand("toggleMap", "Toggle Map View", toggleMap));
			Namespace.AddCommand(new VoiceCommand("cameraFreeMode", "Set Camera to Free Mode", () => setCameraMode(FlightCamera.Modes.FREE)));
			Namespace.AddCommand(new VoiceCommand("cameraChaseMode", "Set Camera to Chase Mode", () => setCameraMode(FlightCamera.Modes.CHASE)));
			Namespace.AddCommand(new VoiceCommand("stage", "Activate Next Stage", activateStage));
			Namespace.AddCommand(new VoiceCommand("throttleFull", "Set Throttle to Full", () => setThrottle(1f)));
			Namespace.AddCommand(new VoiceCommand("throttleZero", "Set Throttle to Zero", () => setThrottle(0f)));
			Namespace.AddCommand(new VoiceCommand("actionGroup1", "Activate Action Group 1", () => toggleActionGroup(KSPActionGroup.Custom01)));
			Namespace.AddCommand(new VoiceCommand("actionGroup2", "Activate Action Group 2", () => toggleActionGroup(KSPActionGroup.Custom02)));
			Namespace.AddCommand(new VoiceCommand("actionGroup3", "Activate Action Group 3", () => toggleActionGroup(KSPActionGroup.Custom03)));
			Namespace.AddCommand(new VoiceCommand("actionGroup4", "Activate Action Group 4", () => toggleActionGroup(KSPActionGroup.Custom04)));
			Namespace.AddCommand(new VoiceCommand("actionGroup5", "Activate Action Group 5", () => toggleActionGroup(KSPActionGroup.Custom05)));
			Namespace.AddCommand(new VoiceCommand("actionGroup6", "Activate Action Group 6", () => toggleActionGroup(KSPActionGroup.Custom06)));
			Namespace.AddCommand(new VoiceCommand("actionGroup7", "Activate Action Group 7", () => toggleActionGroup(KSPActionGroup.Custom07)));
			Namespace.AddCommand(new VoiceCommand("actionGroup8", "Activate Action Group 8", () => toggleActionGroup(KSPActionGroup.Custom08)));
			Namespace.AddCommand(new VoiceCommand("actionGroup9", "Activate Action Group 9", () => toggleActionGroup(KSPActionGroup.Custom09)));
			Namespace.AddCommand(new VoiceCommand("actionGroup10", "Activate Action Group 10", () => toggleActionGroup(KSPActionGroup.Custom10)));
			Namespace.AddCommand(new VoiceCommand("actionGroupGear", "Toggle Gear", () => toggleActionGroup(KSPActionGroup.Gear)));
			Namespace.AddCommand(new VoiceCommand("actionGroupBrakes", "Toggle Brakes", () => toggleActionGroup(KSPActionGroup.Brakes)));
			Namespace.AddCommand(new VoiceCommand("actionGroupLight", "Toggle Light", () => toggleActionGroup(KSPActionGroup.Light)));
			Namespace.AddCommand(new VoiceCommand("actionGroupAbort", "Activate Action Group 'Abort'", () => toggleActionGroup(KSPActionGroup.Abort)));
			Namespace.AddCommand(new VoiceCommand("actionGroupSAS", "Toggle SAS", () => toggleActionGroup(KSPActionGroup.SAS)));
			Namespace.AddCommand(new VoiceCommand("actionGroupRCS", "Toggle RCS", () => toggleActionGroup(KSPActionGroup.RCS)));
		}

		private void quickSave() {
			if (HighLogic.LoadedSceneIsFlight) {
				QuickSaveLoad.QuickSave();
			}
		}

		private void quickLoad() {
			if (HighLogic.LoadedSceneIsFlight) {
				HighLogic.CurrentGame = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
				HighLogic.CurrentGame.startScene = GameScenes.FLIGHT;
				HighLogic.CurrentGame.Start();
			}
		}

		private void toggleMap() {
			if (HighLogic.LoadedSceneIsFlight) {
				if (MapView.MapIsEnabled) {
					MapView.ExitMapView();
				} else {
					MapView.EnterMapView();
				}
			}
		}

		private void setCameraMode(FlightCamera.Modes mode) {
			if (HighLogic.LoadedSceneIsFlight) {
				FlightCamera.fetch.setMode(mode);
			}
		}

		private void activateStage() {
			if (HighLogic.LoadedSceneIsFlight) {
				Staging.ActivateNextStage();
			}
		}

		private void setThrottle(float throttle) {
			if (HighLogic.LoadedSceneIsFlight) {
				FlightInputHandler.state.mainThrottle = throttle;
			}
		}

		private void toggleActionGroup(KSPActionGroup group) {
			if (HighLogic.LoadedSceneIsFlight) {
				FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(group);
			}
		}
	}
}
