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
		private const double WARP_LEAD_TIME = 30;

		private VoiceCommandNamespace ns;
		private bool warping;
		private double warpTargetUT;
		private double warpTargetLeadTime;

		private void Start() {
			Debug.Log("[VoiceCommanderMechJeb] registering commands");

			ns = new VoiceCommandNamespace("mechJeb", "MechJeb");
			ns += new VoiceCommand("turnFlightDirection", "Turn into a Flight Direction", turnFlightDirection);
			ns += new VoiceCommand("killRotation", "Kill Rotation", killRotation);
			ns += new VoiceCommand("turnAxis", "Turn About an Axis", turnAxis);
			ns += new VoiceCommand("createManeuverNodeCircularize", "Create Maneuver Node to Circularize Orbit", createManeuverNodeCircularize);
			ns += new VoiceCommand("executeManeuverNode", "Execute Maneuver Node", executeManeuverNode);
			ns += new VoiceCommand("stopExecutingManeuverNode", "Stop Executing Maneuver Node", stopExecutingManeuverNode);
			ns += new VoiceCommand("removeAllManeuverNodes", "Remove All Maneuver Nodes", removeAllManeuverNodes);
			ns += new VoiceCommand("warpTo", "Time Warp to An Event", warpTo);
			ns += new VoiceCommand("killWarp", "Kill Time Warping", killWarp);

			VoiceCommander.VoiceCommander.Instance.AddNamespace(ns);
		}

		private void OnDestroy() {
			Debug.Log("[VoiceCommanderMechJeb] unregistering commands");
			VoiceCommander.VoiceCommander.Instance.RemoveNamespace(ns);
		}

		private void turnFlightDirection(VoiceCommandRecognizedEvent @event) {
			Vector3d direction = Vector3d.zero;
			switch (@event.Parameters["flightDirection"]) {
				case "prograde":
					direction = Vector3d.forward;
					break;
				case "retrograde":
					direction = Vector3d.back;
					break;
				case "normal":
					direction = Vector3d.left;
					break;
				case "antiNormal":
					direction = Vector3d.right;
					break;
				case "radial":
					direction = Vector3d.up;
					break;
				case "antiRadial":
					direction = Vector3d.down;
					break;
			}
			if (direction != Vector3d.zero) {
				attitude(direction);
			}
		}

		private void attitude(Vector3d direction) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.attitudeTo(direction, AttitudeReference.ORBIT, this);
			}
		}

		private void killRotation(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.users.Remove(this);
				FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
			}
		}

		private void turnAxis(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				string axis = @event.Parameters["axis"];
				string plusMinus = @event.Parameters["plusMinus"];
				string degreesNumber = @event.Parameters["degreesNumber"];
				int degrees = int.Parse(degreesNumber);
				if (plusMinus == "-") {
					degrees = -degrees;
				}

				Vector3 rotationAxis = Vector3.zero;
				switch (axis) {
					case "yaw":
						rotationAxis = Vector3d.down.xzy;
						break;
					case "pitch":
						rotationAxis = Vector3d.right.xzy;
						break;
					case "roll":
						rotationAxis = Vector3d.back.xzy;
						break;
				}

				Transform transform = mechJeb.vessel.GetTransform();
				Vector3 worldRotationAxis = transform.TransformDirection(rotationAxis);
				Quaternion delta = Quaternion.AngleAxis(degrees, worldRotationAxis);
				Quaternion currentRotation = Quaternion.LookRotation(transform.up, -transform.forward);
				Quaternion targetRotation = delta * currentRotation;
				mechJeb.attitude.attitudeTo(targetRotation, AttitudeReference.INERTIAL, this);
			}
		}

		private void createManeuverNodeCircularize(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				MechJebModuleManeuverPlanner.TimeReference time = MechJebModuleManeuverPlanner.TimeReference.COMPUTED;
				switch (@event.Parameters["apPe"]) {
					case "ap":
						time = MechJebModuleManeuverPlanner.TimeReference.APOAPSIS;
						break;
					case "pe":
						time = MechJebModuleManeuverPlanner.TimeReference.PERIAPSIS;
						break;
				}
				if (time != MechJebModuleManeuverPlanner.TimeReference.COMPUTED) {
					MechJebModuleManeuverPlanner planner = mechJeb.GetComputerModule<MechJebModuleManeuverPlanner>();
					MechJebModuleManeuverPlanner.NodePlanningResult planResult = planner.PlanNode(
						MechJebModuleManeuverPlanner.Operation.CIRCULARIZE, time,
						0, 0, 0, 0, 0, 0, 0);
				}
			}
		}

		private void executeManeuverNode(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.node.ExecuteOneNode(this);
			}
		}

		private void stopExecutingManeuverNode(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.node.Abort();
			}
		}

		private void removeAllManeuverNodes(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.vessel.RemoveAllManeuverNodes();
			}
		}

		private void warpTo(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				double targetUT = 0;
				switch (@event.Parameters["warpTarget"]) {
					case "maneuverNode":
						if (mechJeb.vessel.patchedConicSolver.maneuverNodes.Any()) {
							targetUT = mechJeb.vessel.patchedConicSolver.maneuverNodes[0].UT;
						}
						break;

					case "ap":
						if (mechJeb.vessel.orbit.eccentricity < 1) {
							targetUT = mechJeb.vessel.orbit.NextApoapsisTime(mechJeb.vesselState.time);
						}
						break;
					
					case "pe":
						targetUT = mechJeb.vessel.orbit.NextPeriapsisTime(mechJeb.vesselState.time);
						break;

					case "SoI":
						if (mechJeb.vessel.orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) {
							targetUT = mechJeb.vessel.orbit.EndUT;
						}
						break;
				}

				if (targetUT != 0) {
					warpTargetUT = targetUT;
					warpTargetLeadTime = WARP_LEAD_TIME;
					warping = true;
				}
			}
		}

		private void killWarp(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if ((mechJeb != null) && warping) {
				warping = false;
				mechJeb.warp.MinimumWarp(true);
			}
		}

		private MechJebCore getMechJeb() {
			// no need to check HighLogic.LoadedSceneIsFlight here
			return FlightGlobals.ActiveVessel.GetMasterMechJeb();
		}

		private void FixedUpdate() {
			if (warping) {
				MechJebCore mechJeb = getMechJeb();
				if (mechJeb != null) {
					double targetUT = warpTargetUT - warpTargetLeadTime;
					if (mechJeb.vesselState.time < targetUT) {
						mechJeb.warp.WarpToUT(targetUT);
					} else {
						warping = false;
						mechJeb.warp.MinimumWarp(true);
					}
				}
			}
		}
	}
}
