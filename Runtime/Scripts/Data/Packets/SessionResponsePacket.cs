/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Inworld.Packet
{
	[Serializable]
	public class SessionResponseEvent
	{
		public LoadSceneResponse loadedScene;
		public LoadSceneResponse loadedCharacters;
	}

	[Serializable]
	public class SessionResponsePacket : InworldPacket
	{
		public SessionResponseEvent sessionControlResponse;
		
		public SessionResponsePacket()
		{
			type = "SESSION_RESPONSE";
			sessionControlResponse = new SessionResponseEvent();
		}
		public SessionResponsePacket(InworldPacket rhs, SessionResponseEvent evt) : base(rhs)
		{
			type = "SESSION_RESPONSE";
			sessionControlResponse = evt;
		}
		public override string ToJson => JsonUtility.ToJson(this);

		public int GetCharacterCount()
		{
			if (sessionControlResponse == null)
				return 0;
			int nCharacter = sessionControlResponse.loadedCharacters?.agents?.Count ?? 0;
			int nSceneCount = sessionControlResponse.loadedScene?.agents?.Count ?? 0;
			return nCharacter + nSceneCount;
		}
	}
}


