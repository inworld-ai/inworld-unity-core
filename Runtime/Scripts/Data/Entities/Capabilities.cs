/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Packet;
using System;
using UnityEngine;

namespace Inworld.Entities
{
    [Serializable]
    public class Capabilities
    {
        public bool audio;
        public bool emotions;
        public bool interruptions;
        public bool narratedActions;
        public bool text;
        public bool triggers;
        public bool phonemeInfo;
        public bool relations;
        public bool debugInfo;
        public bool multiAgent;

        public Capabilities() {}
        public Capabilities(Capabilities rhs)
        {
            audio = rhs.audio;
            emotions = rhs.emotions;
            interruptions = rhs.interruptions;
            narratedActions = rhs.narratedActions;
            text = rhs.text;
            triggers = rhs.triggers;
            phonemeInfo = rhs.phonemeInfo;
            relations = rhs.relations;
            debugInfo = rhs.debugInfo;
            multiAgent = rhs.multiAgent;
        }
        public void CopyFrom(Capabilities rhs)
        {
            audio = rhs.audio;
            emotions = rhs.emotions;
            interruptions = rhs.interruptions;
            narratedActions = rhs.narratedActions;
            text = rhs.text;
            triggers = rhs.triggers;
            phonemeInfo = rhs.phonemeInfo;
            relations = rhs.relations;
            debugInfo = rhs.debugInfo;
            multiAgent = rhs.multiAgent;
        }
        public CapabilityPacket ToPacket => new CapabilityPacket
        {
            timestamp = InworldDateTime.UtcNow,
            type = "SESSION_CONTROL",
            packetId = new PacketId(),
            routing = new Routing(),
            sessionControl = new CapabilityEvent
            {
                capabilitiesConfiguration = this
            }
        };
        public override string ToString()
        {
            string result = "";
            if (audio)
                result += "AUDIO ";
            if (emotions)
                result += "EMOTIONS ";
            if (interruptions)
                result += "INTERRUPTIONS ";
            if (narratedActions)
                result += "NARRATIVE ";
            if (text)
                result += "TEXT ";
            if (triggers)
                result += "TRIGGER ";
            if (phonemeInfo)
                result += "PHONEME ";
            if (relations)
                result += "RELATIONS ";
            if (multiAgent)
                result += "MULTIAGENT";
            return result;
        }
    }
    [Serializable]
    public class CapabilityEvent
    {
        public Capabilities capabilitiesConfiguration;
    }
    [Serializable]
    public class CapabilityPacket : InworldPacket
    {
        public CapabilityEvent sessionControl;

        public override string ToJson => JsonUtility.ToJson(this);
    }
}
