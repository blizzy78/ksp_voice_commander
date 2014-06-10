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
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class KSPFlightCommands : MonoBehaviour {
		private VoiceCommandNamespace ns;
		private Vessel[] vessels = new Vessel[0];

		private void Start() {
			Debug.Log("[VoiceCommander] registering KSP flight commands");

			ns = new VoiceCommandNamespace("ksp", "KSP");
			ns += new VoiceCommand("quicksave", "Quick Save", quickSave);
			ns += new VoiceCommand("quickload", "Quick Load", quickLoad);
			ns += new VoiceCommand("toggleMap", "Toggle Map View", toggleMap);
			ns += new VoiceCommand("cameraAutoMode", "Set Camera to Auto Mode", (e) => setCameraMode(FlightCamera.Modes.AUTO));
			ns += new VoiceCommand("cameraFreeMode", "Set Camera to Free Mode", (e) => setCameraMode(FlightCamera.Modes.FREE));
			ns += new VoiceCommand("cameraChaseMode", "Set Camera to Chase Mode", (e) => setCameraMode(FlightCamera.Modes.CHASE));
			ns += new VoiceCommand("stage", "Activate Next Stage", activateStage);
			ns += new VoiceCommand("throttleFull", "Set Throttle to Full", (e) => setThrottle(1f));
			ns += new VoiceCommand("throttleZero", "Set Throttle to Zero", (e) => setThrottle(0f));
			ns += new VoiceCommand("throttlePercent", "Set Throttle", throttlePercent);
			ns += new VoiceCommand("actionGroup", "Activate Action Group", toggleActionGroup);
			ns += new VoiceCommand("actionGroupGear", "Toggle Gear", (e) => toggleActionGroup(KSPActionGroup.Gear));
			ns += new VoiceCommand("actionGroupBrakes", "Toggle Brakes", (e) => toggleActionGroup(KSPActionGroup.Brakes));
			ns += new VoiceCommand("actionGroupLight", "Toggle Light", (e) => toggleActionGroup(KSPActionGroup.Light));
			ns += new VoiceCommand("actionGroupAbort", "Activate Action Group 'Abort'", (e) => toggleActionGroup(KSPActionGroup.Abort));
			ns += new VoiceCommand("actionGroupSAS", "Toggle SAS", (e) => toggleActionGroup(KSPActionGroup.SAS));
			ns += new VoiceCommand("actionGroupRCS", "Toggle RCS", (e) => toggleActionGroup(KSPActionGroup.RCS));
			ns += new VoiceCommand("toggleNavBall", "Toggle Nav Ball", toggleNavBall);
			ns += new VoiceCommand("switchToVessel", "Switch to Vessel", switchToVessel);
			ns += new VoiceCommand("switchToNextVessel", "Switch to Next Vessel", switchToNextVessel);
			ns += new VoiceCommand("toggleSolarPanels", "Toggle All Solar Panels", toggleSolarPanels);

			VoiceCommand pauseCommand = new VoiceCommand("pause", "Toggle Game Pause", pause);
			pauseCommand.ExecuteAlways = true;
			ns += pauseCommand;

			VoiceCommander.Instance.AddNamespace(ns);

			updateVesselNameMacroValues();

			GameEvents.onVesselLoaded.Add(vesselLoaded);
			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselCreate.Add(vesselCreate);
			GameEvents.onVesselDestroy.Add(vesselDestroy);
			GameEvents.onVesselRename.Add(vesselRename);
		}

		private void OnDestroy() {
			Debug.Log("[VoiceCommander] unregistering KSP flight commands");

			GameEvents.onVesselLoaded.Remove(vesselLoaded);
			GameEvents.onVesselChange.Remove(vesselChange);
			GameEvents.onVesselCreate.Remove(vesselCreate);
			GameEvents.onVesselDestroy.Remove(vesselDestroy);
			GameEvents.onVesselRename.Remove(vesselRename);

			VoiceCommander.Instance.RemoveNamespace(ns);
		}

		private void vesselLoaded(Vessel vessel) {
			updateVesselNameMacroValues();
		}

		private void vesselChange(Vessel vessel) {
			updateVesselNameMacroValues();
		}

		private void vesselCreate(Vessel vessel) {
			updateVesselNameMacroValues();
		}

		private void vesselDestroy(Vessel vessel) {
			updateVesselNameMacroValues();
		}

		private void vesselRename(GameEvents.HostedFromToAction<Vessel, string> action) {
			updateVesselNameMacroValues();
		}

		private void updateVesselNameMacroValues() {
			List<string> oldNames = new List<string>(this.vessels.Select(v => v.vesselName));

			List<Vessel> newVessels = new List<Vessel>();
			foreach (Vessel vessel in FlightGlobals.Vessels.Where(v => canSwitchTo(v))) {
				newVessels.Add(vessel);
			}

			newVessels.Sort((v1, v2) => oldNames.IndexOf(v1.vesselName) - oldNames.IndexOf(v2.vesselName));

			List<string> newNames = new List<string>(newVessels.Select(v => v.vesselName));

			if (!newNames.SequenceEqual(oldNames)) {
				this.vessels = newVessels.ToArray();
				VoiceCommander.Instance.Vessels = this.vessels;
				VoiceCommander.Instance.SetMacroValueTexts(ns, "vesselName", newNames.ToArray());
			}
		}

		private bool canSwitchTo(Vessel vessel) {
			VesselType type = vessel.vesselType;
			return (type != VesselType.Debris) && (type != VesselType.Flag) && (type != VesselType.SpaceObject) && (type != VesselType.Unknown);
		}

		private void quickSave(VoiceCommandRecognizedEvent @event) {
			QuickSaveLoad.QuickSave();
		}

		private void quickLoad(VoiceCommandRecognizedEvent @event) {
			HighLogic.CurrentGame = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
			HighLogic.CurrentGame.startScene = GameScenes.FLIGHT;
			HighLogic.CurrentGame.Start();
		}

		private void toggleMap(VoiceCommandRecognizedEvent @event) {
			if (MapView.MapIsEnabled) {
				MapView.ExitMapView();
			} else {
				MapView.EnterMapView();
			}
		}

		private void setCameraMode(FlightCamera.Modes mode) {
			FlightCamera.fetch.setMode(mode);
		}

		private void activateStage(VoiceCommandRecognizedEvent @event) {
			Staging.ActivateNextStage();
		}

		private void throttlePercent(VoiceCommandRecognizedEvent @event) {
			setThrottle(float.Parse(@event.Parameters["percentNumber"]) / 100f);
		}

		private void setThrottle(float throttle) {
			FlightInputHandler.state.mainThrottle = throttle;
		}

		private void toggleActionGroup(VoiceCommandRecognizedEvent @event) {
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

		private void toggleActionGroup(KSPActionGroup group) {
			FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(group);
		}

		private void pause(VoiceCommandRecognizedEvent @event) {
			if (FlightDriver.Pause) {
				PauseMenu.Close();
			} else {
				PauseMenu.Display();
			}
		}

		private void toggleNavBall(VoiceCommandRecognizedEvent @event) {
			if (FlightUIModeController.Instance.navBall.expanded) {
				FlightUIModeController.Instance.navBall.Collapse();
			} else {
				FlightUIModeController.Instance.navBall.Expand();
			}
		}

		private void switchToVessel(VoiceCommandRecognizedEvent @event) {
			int idx = int.Parse(@event.Parameters["vesselName"]);
			FlightGlobals.SetActiveVessel(vessels[idx]);
		}

		private void switchToNextVessel(VoiceCommandRecognizedEvent @event) {
			Vessel current = FlightGlobals.ActiveVessel;
			Vessel next = FlightGlobals.FindNearestVesselWhere(current.GetWorldPos3D(), v => !v.Equals(current) && v.IsControllable).FirstOrDefault();
			if (next != null) {
				FlightGlobals.SetActiveVessel(next);
			}
		}

		private void toggleSolarPanels(VoiceCommandRecognizedEvent @event) {
			foreach (ModuleDeployableSolarPanel panel in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>()) {
				if ((panel.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED) && panel.retractable) {
					panel.Retract();
				} else if (panel.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTED) {
					panel.Extend();
				}
			}
		}
	}
}
