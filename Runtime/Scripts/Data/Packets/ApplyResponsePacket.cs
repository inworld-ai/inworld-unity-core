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
	public class ApplyResponse
	{
		public PacketId packetId;
	}
	[Serializable]
	public class ApplyResponseEvent
	{
		public ApplyResponse applyResponse;
	}
	
	[Serializable]
	public class ApplyResponsePacket : InworldPacket
	{
		public ApplyResponseEvent mutation;
		
		public ApplyResponsePacket()
		{
			type = "MUTATION";
			mutation = new ApplyResponseEvent();
		}
		public ApplyResponsePacket(InworldPacket rhs, ApplyResponseEvent evt) : base(rhs)
		{
			type = "MUTATION";
			mutation = evt;
		}
	}
}
