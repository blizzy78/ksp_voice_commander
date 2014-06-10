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
			ns += new VoiceCommand("turnFlightDirection", "Turn into a Flight Direction", "turn <flightDirection>", turnFlightDirection);
			ns += new VoiceCommand("killRotation", "Kill Rotation", "kill rotation", killRotation);
			ns += new VoiceCommand("turnAxis", "Rotate About an Axis", "rotate <axis> <plusMinus> <degreesNumber>", rotateAxis);
			ns += new VoiceCommand("createManeuverNodeCircularize", "Create Maneuver Node to Circularize Orbit", "circularize orbit at <apPe>", createManeuverNodeCircularize);
			ns += new VoiceCommand("executeManeuverNode", "Execute Maneuver Node", "execute maneuver", executeManeuverNode);
			ns += new VoiceCommand("stopExecutingManeuverNode", "Stop Executing Maneuver Node", "stop maneuver", stopExecutingManeuverNode);
			ns += new VoiceCommand("removeAllManeuverNodes", "Remove All Maneuver Nodes", "remove maneuver", removeAllManeuverNodes);
			ns += new VoiceCommand("warpTo", "Time Warp to an Event", "time warp to <warpTarget>", warpTo);
			ns += new VoiceCommand("killWarp", "Kill Time Warping", "stop time warp", killWarp);
			ns += new VoiceCommand("setTarget", "Set Target", "target <vesselName>", setTarget);
			ns += new VoiceCommand("unsetTarget", "Unset Target", "unset target", unsetTarget);
			ns += new VoiceCommand("keepVerticalSpeed", "Keep Vertical Speed", "vertical <plusMinus> <speedNumber>", keepVerticalSpeed);
			ns += new VoiceCommand("stopKeepVerticalSpeed", "Stop Keeping Vertical Speed", "vertical stop", stopKeepVerticalSpeed);
			ns += new VoiceCommand("toggleKillHorizontalSpeed", "Toggle Killing Horizontal Speed", "kill horizontal", toggleKillHorizontalSpeed);

			VoiceCommander.VoiceCommander.Instance.AddNamespace(ns);
		}

		private void OnDestroy() {
			Debug.Log("[VoiceCommanderMechJeb] unregistering commands");
			VoiceCommander.VoiceCommander.Instance.RemoveNamespace(ns);
		}

		private void turnFlightDirection(VoiceCommandRecognizedEvent @event) {
			Vector3d direction = Vector3d.zero;
			AttitudeReference reference = AttitudeReference.ORBIT;
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
				case "maneuverNode":
					direction = Vector3d.forward;
					reference = AttitudeReference.MANEUVER_NODE;
					break;

			}
			if (direction != Vector3d.zero) {
				attitude(direction, reference);
			}
		}

		private void attitude(Vector3d direction, AttitudeReference reference) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.attitudeTo(direction, reference, this);
			}
		}

		private void killRotation(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.attitude.users.Remove(this);
				FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
			}
		}

		private void rotateAxis(VoiceCommandRecognizedEvent @event) {
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

		private void setTarget(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				if (@event.Parameters.ContainsKey("vesselName")) {
					int idx = int.Parse(@event.Parameters["vesselName"]);
					mechJeb.target.Set(VoiceCommander.VoiceCommander.Instance.Vessels[idx]);
				}
			}
		}

		private void unsetTarget(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.target.Unset();
			}
		}

		private void keepVerticalSpeed(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				string plusMinus = @event.Parameters["plusMinus"];
				int speed = int.Parse(@event.Parameters["speed"]);
				int speedDecimal = int.Parse(@event.Parameters["speedDecimal"]);
				float targetSpeed = (float) speed + (float) speedDecimal / 10f;
				if (plusMinus == "-") {
					targetSpeed = -targetSpeed;
				}
				mechJeb.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
				mechJeb.thrust.trans_spd_act = targetSpeed;
				mechJeb.thrust.users.Add(this);
			}
		}

		private void stopKeepVerticalSpeed(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.thrust.tmode = MechJebModuleThrustController.TMode.OFF;
				mechJeb.thrust.ThrustOff();
				mechJeb.thrust.users.Remove(this);
			}
		}

		private void toggleKillHorizontalSpeed(VoiceCommandRecognizedEvent @event) {
			MechJebCore mechJeb = getMechJeb();
			if (mechJeb != null) {
				mechJeb.thrust.trans_kill_h = !mechJeb.thrust.trans_kill_h;
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
