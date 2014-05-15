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
using Toolbar;
using UnityEngine;

namespace VoiceCommander {
	internal class InfoDrawable : IDrawable {
		private const long DISLAY_INTERVAL = 3000;

		private int id = UnityEngine.Random.Range(0, int.MaxValue);
		private IButton button;
		private string text;
		private long creationTime;
		private Rect rect = new Rect(0, 0, 0, 0);

		internal InfoDrawable(IButton button, string text) {
			this.button = button;
			this.text = text;

			creationTime = DateTime.UtcNow.Ticks / 10000;
		}

		public Vector2 Draw(Vector2 position) {
			rect.x = position.x;
			rect.y = position.y;
			rect = GUILayout.Window(id, rect, (windowId) => drawContents(), (string) null, GUI.skin.box);
			return new Vector2(rect.width, rect.height);
		}

		private void drawContents() {
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.wordWrap = false;
			GUILayout.Label(text, style);
		}

		public void Update() {
			long now = DateTime.UtcNow.Ticks / 10000;
			if ((now - creationTime) >= DISLAY_INTERVAL) {
				button.Drawable = null;
			}
		}
	}
}
