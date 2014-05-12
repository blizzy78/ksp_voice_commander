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
	public enum PacketType {
		SPEECH_RECOGNIZED = 1,
		CLEAR_COMMANDS = 2,
		END_OF_COMMANDS = 3,
		ADD_COMMAND = 4,
		SET_YAW_COMMAND = 5,
		SET_PITCH_COMMAND = 6,
		SET_ROLL_COMMAND = 7,
		SET_PROGRADE_COMMAND = 8,
		SET_RETROGRADE_COMMAND = 9,
		SET_NORMAL_COMMAND = 10,
		SET_ANTI_NORMAL_COMMAND = 11,
		SET_RADIAL_COMMAND = 12,
		SET_ANTI_RADIAL_COMMAND = 13,
		SET_APOAPSIS_COMMAND = 14,
		SET_PERIAPSIS_COMMAND = 15
	}

	public class VoicePacket {
		public PacketType Type {
			get;
			private set;
		}

		public string Data {
			get;
			private set;
		}

		public string PacketDataString {
			get {
				return ((int) Type) + "|" + (Data ?? string.Empty);
			}
		}

		public byte[] PacketData {
			get {
				return Encoding.UTF8.GetBytes(PacketDataString);
			}
		}

		public VoicePacket(PacketType type, string data = null) {
			this.Type = type;
			this.Data = data;
		}

		public static VoicePacket FromPacket(byte[] packetData) {
			string dataStr = Encoding.UTF8.GetString(packetData);
			int pos = dataStr.IndexOf('|');
			PacketType type = (PacketType) int.Parse(dataStr.Substring(0, pos));
			string data = dataStr.Substring(pos + 1);
			if (data == string.Empty) {
				data = null;
			}
			return new VoicePacket(type, data);
		}
	}
}
