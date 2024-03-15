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
    public class MutationEvent
    {
        public CancelResponse cancelResponses;
    }
    [Serializable]
    public class MutationPacket : InworldPacket
    {
        public MutationEvent mutation;
        
        public MutationPacket()
        {
            type = "MUTATION";
            mutation = new MutationEvent();
        }
        public MutationPacket(InworldPacket rhs, MutationEvent evt) : base(rhs)
        {
            type = "MUTATION";
            mutation = evt;
        }
        public override string ToJson => JsonUtility.ToJson(this); 
    }
}
