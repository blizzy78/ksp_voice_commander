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
Namespace.AddCommand(
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
			Namespace += new VoiceCommand("quicksave", "Quick Save", quickSave);
			Namespace += new VoiceCommand("quickload", "Quick Load", quickLoad);
			Namespace += new VoiceCommand("toggleMap", "Toggle Map View", toggleMap);
			Namespace += new VoiceCommand("cameraAutoMode", "Set Camera to Auto Mode", (e) => setCameraMode(FlightCamera.Modes.AUTO));
			Namespace += new VoiceCommand("cameraFreeMode", "Set Camera to Free Mode", (e) => setCameraMode(FlightCamera.Modes.FREE));
			Namespace += new VoiceCommand("cameraChaseMode", "Set Camera to Chase Mode", (e) => setCameraMode(FlightCamera.Modes.CHASE));
			Namespace += new VoiceCommand("stage", "Activate Next Stage", activateStage);
			Namespace += new VoiceCommand("throttleFull", "Set Throttle to Full", (e) => setThrottle(1f));
			Namespace += new VoiceCommand("throttleZero", "Set Throttle to Zero", (e) => setThrottle(0f));
			Namespace += new VoiceCommand("throttlePercent", "Set Throttle", throttlePercent);
			Namespace += new VoiceCommand("actionGroup", "Activate Action Group", toggleActionGroup);
			Namespace += new VoiceCommand("actionGroupGear", "Toggle Gear", (e) => toggleActionGroup(KSPActionGroup.Gear));
			Namespace += new VoiceCommand("actionGroupBrakes", "Toggle Brakes", (e) => toggleActionGroup(KSPActionGroup.Brakes));
			Namespace += new VoiceCommand("actionGroupLight", "Toggle Light", (e) => toggleActionGroup(KSPActionGroup.Light));
			Namespace += new VoiceCommand("actionGroupAbort", "Activate Action Group 'Abort'", (e) => toggleActionGroup(KSPActionGroup.Abort));
			Namespace += new VoiceCommand("actionGroupSAS", "Toggle SAS", (e) => toggleActionGroup(KSPActionGroup.SAS));
			Namespace += new VoiceCommand("actionGroupRCS", "Toggle RCS", (e) => toggleActionGroup(KSPActionGroup.RCS));
		}

		private void quickSave(VoiceCommandRecognizedEvent @event) {
			if (HighLogic.LoadedSceneIsFlight) {
				QuickSaveLoad.QuickSave();
			}
		}

		private void quickLoad(VoiceCommandRecognizedEvent @event) {
			if (HighLogic.LoadedSceneIsFlight) {
				HighLogic.CurrentGame = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
				HighLogic.CurrentGame.startScene = GameScenes.FLIGHT;
				HighLogic.CurrentGame.Start();
			}
		}

		private void toggleMap(VoiceCommandRecognizedEvent @event) {
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

		private void activateStage(VoiceCommandRecognizedEvent @event) {
			if (HighLogic.LoadedSceneIsFlight) {
				Staging.ActivateNextStage();
			}
		}

		private void throttlePercent(VoiceCommandRecognizedEvent @event) {
			if (HighLogic.LoadedSceneIsFlight) {
				setThrottle(float.Parse(@event.Parameters["percentNumber"]) / 100f);
			}
		}

		private void setThrottle(float throttle) {
			if (HighLogic.LoadedSceneIsFlight) {
				FlightInputHandler.state.mainThrottle = throttle;
			}
		}

		private void toggleActionGroup(VoiceCommandRecognizedEvent @event) {
			if (HighLogic.LoadedSceneIsFlight) {
				KSPActionGroup group = KSPActionGroup.None;
				switch (@event.Parameters["actionGroupNumber"]) {
					case "1":
						group = KSPActionGroup.Custom01;
						break;
					case "2":
						group = KSPActionGroup.Custom02;
						break;
					case "3":
						group = KSPActionGroup.Custom03;
						break;
					case "4":
						group = KSPActionGroup.Custom04;
						break;
					case "5":
						group = KSPActionGroup.Custom05;
						break;
					case "6":
						group = KSPActionGroup.Custom06;
						break;
					case "7":
						group = KSPActionGroup.Custom07;
						break;
					case "8":
						group = KSPActionGroup.Custom08;
						break;
					case "9":
						group = KSPActionGroup.Custom09;
						break;
					case "10":
						group = KSPActionGroup.Custom10;
						break;
				}
				if (group != KSPActionGroup.None) {
					toggleActionGroup(group);
				}
			}
		}

		private void toggleActionGroup(KSPActionGroup group) {
			if (HighLogic.LoadedSceneIsFlight) {
				FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(group);
			}
		}
	}
}
