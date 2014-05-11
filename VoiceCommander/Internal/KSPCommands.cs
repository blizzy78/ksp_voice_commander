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
			Namespace.AddCommand(new VoiceCommand("quicksave", "Quick Save", kspQuickSave));
			Namespace.AddCommand(new VoiceCommand("quickload", "Quick Load", kspQuickLoad));
			Namespace.AddCommand(new VoiceCommand("toggleMap", "Toggle Map View", kspToggleMap));
			Namespace.AddCommand(new VoiceCommand("cameraFreeMode", "Set Camera to Free Mode", () => kspCameraMode(FlightCamera.Modes.FREE)));
			Namespace.AddCommand(new VoiceCommand("cameraChaseMode", "Set Camera to Chase Mode", () => kspCameraMode(FlightCamera.Modes.CHASE)));
			Namespace.AddCommand(new VoiceCommand("stage", "Activate Next Stage", kspStage));
			Namespace.AddCommand(new VoiceCommand("throttleFull", "Set Throttle to Full", () => kspThrottle(1f)));
			Namespace.AddCommand(new VoiceCommand("throttleZero", "Set Throttle to Zero", () => kspThrottle(0f)));
			Namespace.AddCommand(new VoiceCommand("actionGroup1", "Activate Action Group 1", () => kspActionGroup(KSPActionGroup.Custom01)));
			Namespace.AddCommand(new VoiceCommand("actionGroup2", "Activate Action Group 2", () => kspActionGroup(KSPActionGroup.Custom02)));
			Namespace.AddCommand(new VoiceCommand("actionGroup3", "Activate Action Group 3", () => kspActionGroup(KSPActionGroup.Custom03)));
			Namespace.AddCommand(new VoiceCommand("actionGroup4", "Activate Action Group 4", () => kspActionGroup(KSPActionGroup.Custom04)));
			Namespace.AddCommand(new VoiceCommand("actionGroup5", "Activate Action Group 5", () => kspActionGroup(KSPActionGroup.Custom05)));
			Namespace.AddCommand(new VoiceCommand("actionGroup6", "Activate Action Group 6", () => kspActionGroup(KSPActionGroup.Custom06)));
			Namespace.AddCommand(new VoiceCommand("actionGroup7", "Activate Action Group 7", () => kspActionGroup(KSPActionGroup.Custom07)));
			Namespace.AddCommand(new VoiceCommand("actionGroup8", "Activate Action Group 8", () => kspActionGroup(KSPActionGroup.Custom08)));
			Namespace.AddCommand(new VoiceCommand("actionGroup9", "Activate Action Group 9", () => kspActionGroup(KSPActionGroup.Custom09)));
			Namespace.AddCommand(new VoiceCommand("actionGroup10", "Activate Action Group 10", () => kspActionGroup(KSPActionGroup.Custom10)));
			Namespace.AddCommand(new VoiceCommand("actionGroupGear", "Activate Action Group 'Gear'", () => kspActionGroup(KSPActionGroup.Gear)));
			Namespace.AddCommand(new VoiceCommand("actionGroupBrakes", "Activate Action Group 'Brakes'", () => kspActionGroup(KSPActionGroup.Brakes)));
			Namespace.AddCommand(new VoiceCommand("actionGroupLight", "Activate Action Group 'Light'", () => kspActionGroup(KSPActionGroup.Light)));
			Namespace.AddCommand(new VoiceCommand("actionGroupAbort", "Activate Action Group 'Abort'", () => kspActionGroup(KSPActionGroup.Abort)));
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
