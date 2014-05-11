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
using System.Threading;
using VoiceCommander;

namespace VoiceServer {
	internal class VoiceServer {
		private const string HOST = "127.0.0.1";
		private const int CLIENT_PORT = 48285;
		private const int SERVER_PORT = 48286;
		private const float MIN_CONFIDENCE = 0.5f;

		private List<string> commands = new List<string>();
		private bool running = true;
		private bool listening = true;
		private SpeechRecognitionEngine engine;
		private IPEndPoint clientEndPoint;
		private IPEndPoint serverEndPoint;
		private UdpClient client;

		public static void Main(string[] args) {
			new VoiceServer();
		}

		private VoiceServer() {
			RecognizerInfo recognizer = SpeechRecognitionEngine.InstalledRecognizers()[0];
			engine = new SpeechRecognitionEngine(recognizer);
			engine.SpeechRecognized += (sender, @event) => speechRecognized(@event.Result);
			engine.SetInputToDefaultAudioDevice();

			clientEndPoint = new IPEndPoint(IPAddress.Parse(HOST), CLIENT_PORT);
			serverEndPoint = new IPEndPoint(IPAddress.Parse(HOST), SERVER_PORT);
			client = new UdpClient(serverEndPoint);
			startReceive();

			Console.WriteLine("Listening... (press Ctrl+C to exit)");

			lock (this) {
				while (running) {
					Monitor.Wait(this);
				}
			}

			client.Close();

			engine.RecognizeAsyncStop();
			engine.Dispose();

			Console.WriteLine("Exiting.");
		}

		private void speechRecognized(RecognitionResult result) {
			if (listening && (result.Confidence >= MIN_CONFIDENCE)) {
				Console.WriteLine(string.Format("Command recognized: {0} (confidence: {1})", result.Text, result.Confidence));
				VoicePacket packet = new VoicePacket(PacketType.SPEECH_RECOGNIZED, string.Format(CultureInfo.InvariantCulture, "{0}", result.Confidence) + "|" + result.Text);
				byte[] data = packet.PacketData;
				client.Send(data, data.Length, clientEndPoint);
			}
		}

		private void startReceive() {
			client.BeginReceive(dataReceived, null);
		}

		private void dataReceived(IAsyncResult result) {
			IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] data = client.EndReceive(result, ref senderEndPoint);
			VoicePacket packet = VoicePacket.FromPacket(data);
			switch (packet.Type) {
				case PacketType.CLEAR_COMMANDS:
					lock (commands) {
						commands.Clear();
					}
					reloadCommands();
					Console.WriteLine("Commands cleared.");
					break;

				case PacketType.ADD_COMMAND:
					lock (commands) {
						commands.Add(packet.Data);
					}
					reloadCommands();
					Console.WriteLine(string.Format("Command added: {0}", packet.Data));
					break;
			}

			string command = Encoding.UTF8.GetString(data);
			lock (commands) {
				commands.Add(command);
			}
			startReceive();
		}

		private void reloadCommands() {
			lock (commands) {
				engine.RecognizeAsyncStop();
				engine.UnloadAllGrammars();

				if (commands.Count > 0) {
					Choices choices = new Choices();
					foreach (string command in commands) {
						choices.Add(command);
					}
					Grammar grammar = new Grammar(choices);

					engine.LoadGrammar(grammar);

					for (;;) {
						try {
							engine.RecognizeAsync(RecognizeMode.Multiple);
							break;
						} catch (Exception) {
							// this can fail if the engine is in the middle of recognizing, just wait a bit and try again
							Thread.Sleep(100);
						}
					}
				}
			}
		}
	}
}
