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
		private const string GENERIC_TEST_COMMAND = "test 1 2 3";

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
		private Choices speedNumberChoices;
		private Choices decimalNumberChoices;
		private Texts texts = new Texts();
		private int regularGroupNumber;
		private int macroGroupNumber;

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
			speedNumberChoices = new Choices();
			decimalNumberChoices = new Choices();
			for (int i = 0; i <= 359; i++) {
				SemanticResultValue value = new SemanticResultValue(i.ToString(), i);

				degreesNumberChoices.Add(value);
				
				if ((i >= 1) && (i <= 10)) {
					actionGroupNumberChoices.Add(value);
				}
				
				if ((i >= 0) && (i <= 100)) {
					percentNumberChoices.Add(value);
				}
				
				if ((i >= 0) && (i <= 10)) {
					speedNumberChoices.Add(value);
				}
				
				if ((i >= 0) && (i <= 9)) {
					decimalNumberChoices.Add(value);
				}
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
			Console.WriteLine(string.Format("You can say '{0}' into your microphone to test now.", GENERIC_TEST_COMMAND));

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
#if DEBUG
			Console.WriteLine(string.Format("Command recognized: {0} (confidence: {1}%)", result.Text, (result.Confidence * 100f).ToString("F1")));
#endif

			if (result.Text != GENERIC_TEST_COMMAND) {
				StringBuilder buf = new StringBuilder();
				if (commandGrammars.ContainsKey(result.Grammar)) {
					buf.Append("command=").Append(commandGrammars[result.Grammar])
						.Append("|confidence=").Append(string.Format(CultureInfo.InvariantCulture, "{0}", result.Confidence));
					foreach (string key in result.Semantics.Select(kv => kv.Key)) {
						buf.Append("|").Append(key).Append("=").Append(result.Semantics[key].Value);
					}
					VoicePacket packet = new VoicePacket(PacketType.SPEECH_RECOGNIZED, buf.ToString());

#if DEBUG
					Console.WriteLine(string.Format("Sending command: {0}", packet.PacketDataString));
#endif
					byte[] data = packet.PacketData;
					try {
						client.Send(data, data.Length, clientEndPoint);
					} catch (Exception) {
						// ignore
					}
				}
			} else {
				Console.WriteLine("Test successful.");
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
				lock (texts) {
					switch (packet.Type) {
						case PacketType.CLEAR_COMMANDS:
							texts.clear();
#if DEBUG
							Console.WriteLine("Commands cleared.");
#endif
							break;

						case PacketType.ADD_COMMAND:
							{
								string[] parts = packet.Data.Split(new char[] { '|' }, 2, StringSplitOptions.RemoveEmptyEntries);
								string fullCmdId = parts[0];
								string text = parts[1];
								List<string> cmdTexts;
								if (texts.commands.ContainsKey(fullCmdId)) {
									cmdTexts = texts.commands[fullCmdId];
								} else {
									cmdTexts = new List<string>();
									texts.commands.Add(fullCmdId, cmdTexts);
								}
								cmdTexts.Add(text);
#if DEBUG
								Console.WriteLine(string.Format("Command added: {0} ({1})", text, fullCmdId));
#endif
							}
							break;

						case PacketType.ADD_MACRO_COMMAND:
							{
								string[] parts = packet.Data.Split(new char[] { '|' }, 2, StringSplitOptions.RemoveEmptyEntries);
								string fullMacroId = parts[0];
								string text = parts[1];
								List<string> valueTexts;
								if (texts.macroValueTexts.ContainsKey(fullMacroId)) {
									valueTexts = texts.macroValueTexts[fullMacroId];
								} else {
									valueTexts = new List<string>();
									texts.macroValueTexts.Add(fullMacroId, valueTexts);
								}
								valueTexts.Add(text);
#if DEBUG
								Console.WriteLine(string.Format("Macro value added: {0} ({1})", text, fullMacroId));
#endif
							}
							break;

						case PacketType.END_OF_COMMANDS:
							reloadCommandsInEngine();
							Console.WriteLine("Reloaded commands.");
							break;

						case PacketType.SET_YAW_COMMAND:
							texts.yawText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'yaw' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_PITCH_COMMAND:
							texts.pitchText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'pitch' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_ROLL_COMMAND:
							texts.rollText = packet.Data;
#if DEBUG	
							Console.WriteLine(string.Format("Set 'roll' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_PROGRADE_COMMAND:
							texts.progradeText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'prograde' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_RETROGRADE_COMMAND:
							texts.retrogradeText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'retrograde' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_NORMAL_COMMAND:
							texts.normalText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'normal' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_ANTI_NORMAL_COMMAND:
							texts.antiNormalText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'anti-normal' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_RADIAL_COMMAND:
							texts.radialText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'radial' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_ANTI_RADIAL_COMMAND:
							texts.antiRadialText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'anti-radial' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_APOAPSIS_COMMAND:
							texts.apoapsisText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'apoapsis' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_PERIAPSIS_COMMAND:
							texts.periapsisText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'periapsis' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_MANEUVER_NODE_COMMAND:
							texts.maneuverNodeText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'maneuver node' command: {0}", packet.Data));
#endif
							break;
						case PacketType.SET_SOI_COMMAND:
							texts.soiText = packet.Data;
#if DEBUG
							Console.WriteLine(string.Format("Set 'sphere of influence' command: {0}", packet.Data));
#endif
							break;
					}
				}
			} catch (Exception) {
				// ignore
			}

			// start receiving next command
			startReceive();
		}

		private void reloadCommandsInEngine() {
			engine.RecognizeAsyncStop();

			commandGrammars.Clear();

#if DEBUG
			StopWatch stopWatch = new StopWatch().start();
#endif

			foreach (KeyValuePair<string, List<string>> cmdEntry in texts.commands) {
				foreach (string text in cmdEntry.Value) {
					GrammarBuilder commandGrammarBuilder = createCommandGrammarBuilder(text);
					if (commandGrammarBuilder != null) {
						Grammar commandGrammar = new Grammar(commandGrammarBuilder);
						commandGrammars.Add(commandGrammar, cmdEntry.Key);
#if DEBUG
						Console.WriteLine(string.Format("Added engine command: {0}", text));
#endif
					} else {
#if DEBUG
						Console.WriteLine(string.Format("Couldn't parse command, ignoring: {0}", text));
#endif
					}
				}
			}

			// generic test command
			GrammarBuilder testCommandGrammarBuilder = createCommandGrammarBuilder(GENERIC_TEST_COMMAND);
			Grammar testCommandGrammar = new Grammar(testCommandGrammarBuilder);
			commandGrammars.Add(testCommandGrammar, "voiceCommander/voiceTest");

#if DEBUG
			Console.WriteLine(string.Format("Creating grammars took {0} ms", stopWatch.elapsed()));
#endif

			for (;;) {
				try {
					engine.UnloadAllGrammars();

#if DEBUG
					stopWatch.start();
#endif
					foreach (Grammar grammar in commandGrammars.Keys) {
						engine.LoadGrammar(grammar);
					}
#if DEBUG
					Console.WriteLine(string.Format("Loading grammars took {0} ms", stopWatch.elapsed()));
#endif

					engine.RecognizeAsync(RecognizeMode.Multiple);
					break;
				} catch (Exception) {
					// this can fail if the engine is still recognizing, just wait a bit and try again
					Thread.Sleep(100);
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
							GrammarBuilder macroChoices = createMacroChoices(capture.Value);
							if (macroChoices != null) {
								commandGrammarBuilder.Append(macroChoices);
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

		private GrammarBuilder createMacroChoices(string macro) {
			switch (macro) {
				case "plusMinus":
					return createSemanticChoicesBuilder(macro, plusMinusChoices);
				case "degreesNumber":
					return createSemanticChoicesBuilder(macro, degreesNumberChoices);
				case "actionGroupNumber":
					return createSemanticChoicesBuilder(macro, actionGroupNumberChoices);
				case "percentNumber":
					return createSemanticChoicesBuilder(macro, percentNumberChoices);

				case "axis":
					lock (texts) {
						if (texts.HaveAxisTexts) {
							return createSemanticChoicesBuilder(macro, new Choices(
								new SemanticResultValue(texts.yawText, "yaw"),
								new SemanticResultValue(texts.pitchText, "pitch"),
								new SemanticResultValue(texts.rollText, "roll")));
						}
					}
					break;

				case "flightDirection":
					lock (texts) {
						if (texts.HaveFlightDirectionTexts) {
							return createSemanticChoicesBuilder(macro, new Choices(
								new SemanticResultValue(texts.progradeText, "prograde"),
								new SemanticResultValue(texts.retrogradeText, "retrograde"),
								new SemanticResultValue(texts.normalText, "normal"),
								new SemanticResultValue(texts.antiNormalText, "antiNormal"),
								new SemanticResultValue(texts.radialText, "radial"),
								new SemanticResultValue(texts.antiRadialText, "antiRadial"),
								new SemanticResultValue(texts.maneuverNodeText, "maneuverNode")));
						}
					}
					break;

				case "apPe":
					lock (texts) {
						if (texts.HaveApPeTexts) {
							return createSemanticChoicesBuilder(macro, new Choices(
								new SemanticResultValue(texts.apoapsisText, "ap"),
								new SemanticResultValue(texts.periapsisText, "pe")));
						}
					}
					break;

				case "warpTarget":
					lock (texts) {
						if (texts.HaveWarpTargetTexts) {
							return createSemanticChoicesBuilder(macro, new Choices(
								new SemanticResultValue(texts.apoapsisText, "ap"),
								new SemanticResultValue(texts.periapsisText, "pe"),
								new SemanticResultValue(texts.maneuverNodeText, "maneuverNode"),
								new SemanticResultValue(texts.soiText, "SoI")));
						}
					}
					break;

				case "speedNumber":
					{
						GrammarBuilder grammar = new GrammarBuilder();
						grammar.Append(new SemanticResultKey("speed", speedNumberChoices));
						grammar.Append(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
						grammar.Append(new SemanticResultKey("speedDecimal", decimalNumberChoices));
						return grammar;
					}

				default:
					lock (texts) {
						List<string> valueTexts = getMacroValueTexts(macro);
						if ((valueTexts != null) && (valueTexts.Count() > 0)) {
							Choices choices = new Choices();
							for (int i = 0; i < valueTexts.Count(); i++) {
								choices.Add(new SemanticResultValue(valueTexts[i], i.ToString()));
							}
							return createSemanticChoicesBuilder(macro, choices);
						}
					}
					break;
			}
			return null;
		}

		private GrammarBuilder createSemanticChoicesBuilder(string semanticKey, Choices choices) {
			return new SemanticResultKey(semanticKey, choices);
		}

		private List<string> getMacroValueTexts(string macroId) {
			if (texts.macroValueTexts.ContainsKey(macroId)) {
				return texts.macroValueTexts[macroId];
			} else {
				string macroIdSuffix = "/" + macroId;
				macroId = texts.macroValueTexts.Keys.FirstOrDefault(k => k.EndsWith(macroIdSuffix));
				if (macroId != null) {
					return texts.macroValueTexts[macroId];
				}
			}
			return null;
		}
	}
}
