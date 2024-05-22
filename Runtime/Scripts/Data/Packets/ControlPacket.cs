/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Inworld.Packet
{
    [Serializable]
    public class AudioSessionPayload
    {
        public string mode;
    }
    [Serializable]
    public class ControlEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ControlType action;
        public string description;
    }
    [Serializable]
    public class AudioControlEvent : ControlEvent
    {
        public AudioSessionPayload audioSessionStart;
    }
    [Serializable]
    public class ConversationControlEvent : ControlEvent
    {
        public ConversationUpdatePayload conversationUpdate;
    }
    [Serializable]
    public class ConversationUpdatePayload
    {
        public List<Source> participants;
    }
    [Serializable]
    public class ControlPacket : InworldPacket
    {
        [JsonConverter(typeof(ControlEventDeserializer))]
        public ControlEvent control;
        public ControlPacket()
        {
            type = PacketType.CONTROL;
            control = new ControlEvent();
        }
        public ControlPacket(InworldPacket rhs, ControlEvent evt) : base(rhs)
        {
            type = PacketType.CONTROL;
            control = evt;
        }
        [JsonIgnore]
        public ControlType Action => control?.action ?? ControlType.UNKNOWN;
    }
}
