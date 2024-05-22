/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using Newtonsoft.Json;
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
		
		[JsonIgnore]
		public bool IsValid => (loadedScene?.IsValid ?? false) || (loadedCharacters?.IsValid ?? false);
	}

	[Serializable]
	public class SessionResponsePacket : InworldPacket
	{
		public SessionResponseEvent sessionControlResponse;
		
		public SessionResponsePacket()
		{
			type = PacketType.SESSION_RESPONSE;
			sessionControlResponse = new SessionResponseEvent();
		}
		public SessionResponsePacket(InworldPacket rhs, SessionResponseEvent evt) : base(rhs)
		{
			type = PacketType.SESSION_RESPONSE;
			sessionControlResponse = evt;
		}
	}
}


