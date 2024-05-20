/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inworld.Packet
{
	[Serializable]
	public class RegenerateResponse
	{
		public string interactionId;
	}
	[Serializable]
	public class RegenerateResponseEvent
	{
		public RegenerateResponse regenerateResponse;
	}
	
	[Serializable]
	public class RegenerateResponsePacket : InworldPacket
	{
		public RegenerateResponseEvent mutation;
		
		public RegenerateResponsePacket()
		{
			type = "MUTATION";
			mutation = new RegenerateResponseEvent();
		}
		public RegenerateResponsePacket(InworldPacket rhs, RegenerateResponseEvent evt) : base(rhs)
		{
			type = "MUTATION";
			mutation = evt;
		}
	}
}
