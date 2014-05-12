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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Speech;
using System.Speech.Recognition;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using VoiceCommander;

namespace VoiceServer {
	internal class VoiceServer {
		private const string HOST = "127.0.0.1";
		private const int CLIENT_PORT = 48285;
		private const int SERVER_PORT = 48286;

		private const string PLAIN_TEXT_PATTERN = "[^\\<\\>]+";
		private const string REGULAR_TEXT_PATTERN = "\\s*(?<regular>" + PLAIN_TEXT_PATTERN + ")\\s*";
		private const string MACRO_PATTERN = "\\s*\\<(?<macro>" + PLAIN_TEXT_PATTERN + ")\\>\\s*";
		private const string COMMAND_PATTERN = "^(" + REGULAR_TEXT_PATTERN + "|" + MACRO_PATTERN + ")+$";

		private bool running = true;
		private object runningMonitor = new object();
		private SpeechRecognitionEngine engine;
		private IPEndPoint clientEndPoint;
		private IPEndPoint serverEndPoint;
		private UdpClient client;
		private Regex commandRegex = new Regex(COMMAND_PATTERN, RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		private Dictionary<Grammar, string> commandGrammars = new Dictionary<Grammar, string>();

		private Choices plusMinusChoices;
		private Choices degreesNumberChoices;
		private Choices actionGroupNumberChoices;
		private Choices percentNumberChoices;
		private Dictionary<string, string> commands = new Dictionary<string, string>();
		private string yawText;
		private string pitchText;
		private string rollText;
		private string progradeText;
		private string retrogradeText;
		private string normalText;
		private string antiNormalText;
		private string radialText;
		private string antiRadialText;
		private string apoapsisText;
		private string periapsisText;
		private int regularGroupNumber;
		private int macroGroupNumber;

		private bool HaveAxisTexts {
			get {
				return !string.IsNullOrEmpty(yawText) && !string.IsNullOrEmpty(pitchText) && !string.IsNullOrEmpty(rollText);
			}
		}

		private bool HaveFlightDirectionTexts {
			get {
				return !string.IsNullOrEmpty(progradeText) && !string.IsNullOrEmpty(retrogradeText) && !string.IsNullOrEmpty(normalText) &&
					!string.IsNullOrEmpty(antiNormalText) && !string.IsNullOrEmpty(radialText) && !string.IsNullOrEmpty(antiRadialText);
			}
		}

		private bool HaveApPeTexts {
			get {
				return !string.IsNullOrEmpty(apoapsisText) && !string.IsNullOrEmpty(periapsisText);
			}
		}

		public static void Main(string[] args) {
			new VoiceServer();
		}

		private VoiceServer() {
			regularGroupNumber = commandRegex.GroupNumberFromName("regular");
			macroGroupNumber = commandRegex.GroupNumberFromName("macro");

			plusMinusChoices = new Choices(new SemanticResultValue("+", "+"), new SemanticResultValue("-", "-"));

			degreesNumberChoices = new Choices();
			actionGroupNumberChoices = new Choices();
			percentNumberChoices = new Choices();
			for (int i = 0; i <= 359; i++) {
				SemanticResultValue value = new SemanticResultValue(i.ToString(), i);
				if ((i >= 1) && (i <= 10)) {
					actionGroupNumberChoices.Add(value);
				}
				if ((i >= 0) && (i <= 100)) {
					percentNumberChoices.Add(value);
				}
				degreesNumberChoices.Add(value);
			}

			RecognizerInfo recognizer = SpeechRecognitionEngine.InstalledRecognizers()[0];
			engine = new SpeechRecognitionEngine(recognizer);
			engine.SpeechRecognized += (sender, @event) => speechRecognized(@event.Result);
			engine.SetInputToDefaultAudioDevice();

			reloadCommandsInEngine();

			clientEndPoint = new IPEndPoint(IPAddress.Parse(HOST), CLIENT_PORT);
			serverEndPoint = new IPEndPoint(IPAddress.Parse(HOST), SERVER_PORT);
			client = new UdpClient(serverEndPoint);
			startReceive();

			Console.WriteLine("Listening... Press Ctrl+C to exit, or close the window.");
			Console.WriteLine("You can say 'test 1 2 3' into your microphone to test now.");

			lock (runningMonitor) {
				while (running) {
					Monitor.Wait(runningMonitor);
				}
			}

			client.Close();

			engine.RecognizeAsyncStop();
			engine.Dispose();

			Console.WriteLine("Exiting.");
		}

		private void speechRecognized(RecognitionResult result) {
			Console.WriteLine(string.Format("Command recognized: {0} (confidence: {1}%)", result.Text, (result.Confidence * 100f).ToString("F1")));

			StringBuilder buf = new StringBuilder();
			if (commandGrammars.ContainsKey(result.Grammar)) {
				buf.Append("command=").Append(commandGrammars[result.Grammar])
					.Append("|confidence=").Append(string.Format(CultureInfo.InvariantCulture, "{0}", result.Confidence));
				foreach (string key in result.Semantics.Select(kv => kv.Key)) {
					buf.Append("|").Append(key).Append("=").Append(result.Semantics[key].Value);
				}
				VoicePacket packet = new VoicePacket(PacketType.SPEECH_RECOGNIZED, buf.ToString());

				Console.WriteLine(string.Format("Sending command: {0}", packet.PacketDataString));
				byte[] data = packet.PacketData;
				try {
					client.Send(data, data.Length, clientEndPoint);
				} catch (Exception) {
					// ignore
				}
			}
		}

		private void startReceive() {
			client.BeginReceive(dataReceived, null);
		}

		private void dataReceived(IAsyncResult result) {
			try {
				IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.EndReceive(result, ref senderEndPoint);
				VoicePacket packet = VoicePacket.FromPacket(data);
				switch (packet.Type) {
					case PacketType.CLEAR_COMMANDS:
						lock (commands) {
							commands.Clear();
						}
						Console.WriteLine("Commands cleared.");
						break;

					case PacketType.ADD_COMMAND:
						lock (commands) {
							string[] parts = packet.Data.Split(new char[] { '|' }, 2, StringSplitOptions.RemoveEmptyEntries);
							commands.Add(parts[0], parts[1]);
							Console.WriteLine(string.Format("Command added: {0} ({1})", parts[1], parts[0]));
						}
						break;

					case PacketType.END_OF_COMMANDS:
						reloadCommandsInEngine();
						break;

					case PacketType.SET_YAW_COMMAND:
						yawText = packet.Data;
						Console.WriteLine(string.Format("Set 'yaw' command: {0}", packet.Data));
						break;
					case PacketType.SET_PITCH_COMMAND:
						pitchText = packet.Data;
						Console.WriteLine(string.Format("Set 'pitch' command: {0}", packet.Data));
						break;
					case PacketType.SET_ROLL_COMMAND:
						rollText = packet.Data;
						Console.WriteLine(string.Format("Set 'roll' command: {0}", packet.Data));
						break;
					case PacketType.SET_PROGRADE_COMMAND:
						progradeText = packet.Data;
						Console.WriteLine(string.Format("Set 'prograde' command: {0}", packet.Data));
						break;
					case PacketType.SET_RETROGRADE_COMMAND:
						retrogradeText = packet.Data;
						Console.WriteLine(string.Format("Set 'retrograde' command: {0}", packet.Data));
						break;
					case PacketType.SET_NORMAL_COMMAND:
						normalText = packet.Data;
						Console.WriteLine(string.Format("Set 'normal' command: {0}", packet.Data));
						break;
					case PacketType.SET_ANTI_NORMAL_COMMAND:
						antiNormalText = packet.Data;
						Console.WriteLine(string.Format("Set 'anti-normal' command: {0}", packet.Data));
						break;
					case PacketType.SET_RADIAL_COMMAND:
						radialText = packet.Data;
						Console.WriteLine(string.Format("Set 'radial' command: {0}", packet.Data));
						break;
					case PacketType.SET_ANTI_RADIAL_COMMAND:
						antiRadialText = packet.Data;
						Console.WriteLine(string.Format("Set 'anti-radial' command: {0}", packet.Data));
						break;
					case PacketType.SET_APOAPSIS_COMMAND:
						apoapsisText = packet.Data;
						Console.WriteLine(string.Format("Set 'apoapsis' command: {0}", packet.Data));
						break;
					case PacketType.SET_PERIAPSIS_COMMAND:
						periapsisText = packet.Data;
						Console.WriteLine(string.Format("Set 'periapsis' command: {0}", packet.Data));
						break;
				}
			} catch (Exception) {
				// ignore
			}

			// start receiving next command
			startReceive();
		}

		private void reloadCommandsInEngine() {
			lock (commands) {
				engine.RecognizeAsyncStop();

				commandGrammars.Clear();

				foreach (KeyValuePair<string, string> cmdEntry in commands) {
					GrammarBuilder commandGrammarBuilder = createCommandGrammarBuilder(cmdEntry.Value);
					if (commandGrammarBuilder != null) {
						Grammar commandGrammar = new Grammar(commandGrammarBuilder);
						commandGrammars.Add(commandGrammar, cmdEntry.Key);
						Console.WriteLine(string.Format("Added engine command: {0}", cmdEntry.Value));
					} else {
						Console.WriteLine(string.Format("Couldn't parse command, ignoring: {0}", cmdEntry.Value));
					}
				}

				// generic test command
				GrammarBuilder testCommandGrammarBuilder = createCommandGrammarBuilder("test 1 2 3");
				Grammar testCommandGrammar = new Grammar(testCommandGrammarBuilder);
				commandGrammars.Add(testCommandGrammar, "voiceCommander/voiceTest");

				for (;;) {
					try {
						engine.UnloadAllGrammars();
						foreach (Grammar grammar in commandGrammars.Keys) {
							engine.LoadGrammar(grammar);
						}
						engine.RecognizeAsync(RecognizeMode.Multiple);
						break;
					} catch (Exception) {
						// this can fail if the engine is still recognizing, just wait a bit and try again
						Thread.Sleep(100);
					}
				}
			}
		}

		private GrammarBuilder createCommandGrammarBuilder(string command) {
			Match match = commandRegex.Match(command);
			if (match.Success) {
				List<Capture> captures = new List<Capture>();
				Dictionary<Capture, int> captureGroups = new Dictionary<Capture, int>();

				int groupNumber = 0;
				foreach (Group group in match.Groups) {
					if ((groupNumber == regularGroupNumber) || (groupNumber == macroGroupNumber)) {
						foreach (Capture capture in group.Captures) {
							captures.Add(capture);
							captureGroups.Add(capture, groupNumber);
						}
					}

					groupNumber++;
				}

				if (captures.Count > 0) {
					captures.Sort((c1, c2) => c1.Index - c2.Index);

					GrammarBuilder commandGrammarBuilder = new GrammarBuilder();
					foreach (Capture capture in captures) {
						groupNumber = captureGroups[capture];
						if (groupNumber == macroGroupNumber) {
							Choices macroChoices = createMacroChoices(capture.Value);
							if (macroChoices != null) {
								commandGrammarBuilder.Append(new SemanticResultKey(capture.Value, macroChoices));
							} else {
								return null;
							}
						} else {
							commandGrammarBuilder.Append(capture.Value);
						}
					}

					return commandGrammarBuilder;
				}
			}
			return null;
		}

		private Choices createMacroChoices(string macro) {
			switch (macro) {
				case "plusMinus":
					return plusMinusChoices;
				case "degreesNumber":
					return degreesNumberChoices;
				case "actionGroupNumber":
					return actionGroupNumberChoices;
				case "percentNumber":
					return percentNumberChoices;

				case "axis":
					if (HaveAxisTexts) {
						return new Choices(
							new SemanticResultValue(yawText, "yaw"),
							new SemanticResultValue(pitchText, "pitch"),
							new SemanticResultValue(rollText, "roll"));
					}
					break;

				case "flightDirection":
					if (HaveFlightDirectionTexts) {
						return new Choices(
							new SemanticResultValue(progradeText, "prograde"),
							new SemanticResultValue(retrogradeText, "retrograde"),
							new SemanticResultValue(normalText, "normal"),
							new SemanticResultValue(antiNormalText, "antiNormal"),
							new SemanticResultValue(radialText, "radial"),
							new SemanticResultValue(antiRadialText, "antiRadial"));
					}
					break;

				case "apPe":
					if (HaveApPeTexts) {
						return new Choices(
							new SemanticResultValue(apoapsisText, "ap"),
							new SemanticResultValue(periapsisText, "pe"));
					}
					break;
			}
			return null;
		}
	}
}
