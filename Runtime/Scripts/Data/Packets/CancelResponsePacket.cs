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
    public class CancelResponse
    {
        public string interactionId;
        public List<string> utteranceId;
    }
    [Serializable]
    public class CancelResponseEvent
    {
        public CancelResponse cancelResponses;
    }
    [Serializable]
    public class CancelResponsePacket : InworldPacket
    {
        public CancelResponseEvent mutation;
        
        public CancelResponsePacket()
        {
            type = PacketType.MUTATION;
            mutation = new CancelResponseEvent();
        }
        public CancelResponsePacket(InworldPacket rhs, CancelResponseEvent evt) : base(rhs)
        {
            type = PacketType.MUTATION;
            mutation = evt;
        }
    }
}
