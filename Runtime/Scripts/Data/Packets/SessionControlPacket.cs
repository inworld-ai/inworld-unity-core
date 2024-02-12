/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using System;

namespace Inworld.Packet
{
	[Serializable]
	public class SessionControlResponseEvent
	{
		public LoadSceneResponse loadScene;
		public LoadSceneResponse loadedCharacters;
	}

}


