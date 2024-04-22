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
    public class ConversationUpdatePayLoad
    {
        public ConversationUpdate conversationUpdate;
    }
    [Serializable]
    public class ConversationUpdate
    {
        public List<Source> participants;
    }
    [Serializable]
    public class ConversationEvent
    {
        public string action;
        public string description;
        public ConversationUpdate conversationUpdate;
        //public string payload;
    }
    [Serializable]
    public class ConversationPacket : InworldPacket
    {
        public ConversationEvent control;

        public ConversationPacket()
        {
            type = "CONTROL";
            control = new ConversationEvent();
        }
        public ConversationPacket(InworldPacket rhs, ConversationEvent evt) : base(rhs)
        {
            type = "CONTROL";
            control = evt;
        }
        public ControlType Action => Enum.TryParse(control.action, true, out ControlType result) ? result : ControlType.UNKNOWN;
        public bool IsConversation => control.action == ControlType.CONVERSATION_UPDATE.ToString();

        public void InstallPayload(List<Source> agentIDs)
        {
            if (control == null || agentIDs == null || agentIDs.Count == 0)
                return;
            packetId.conversationId = InworldController.CharacterHandler.ConversationID;
            control.action = ControlType.CONVERSATION_UPDATE.ToString();
            control.conversationUpdate = new ConversationUpdate
            {
                participants = agentIDs
            };
        }
        public override string ToJson => JsonUtility.ToJson(this); 
    }
}
