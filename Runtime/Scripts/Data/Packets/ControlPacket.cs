/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Inworld.Packet
{
    public class ControlEventDeserializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Not used. 
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["audioSessionStart"] != null)
                return jo.ToObject<AudioControlEvent>(serializer);
            if (jo["conversationUpdate"] != null)
                return jo.ToObject<ConversationControlEvent>(serializer);
            if (jo["sessionControl"] != null)
                return jo.ToObject<SessionControlEvent>(serializer);
            return jo.ToObject<ControlEvent>(serializer);
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ControlEvent);
        }
        public override bool CanWrite => false; // YAN: Use default serializer.
    }
    public class ControlEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ControlType action;
        public string description;
    }
    public class AudioControlEvent : ControlEvent
    {
        public AudioSessionPayload audioSessionStart;
    }
    public class AudioSessionPayload
    {
        public string mode;
    }
    public class ConversationControlEvent : ControlEvent
    {
        public ConversationUpdatePayload conversationUpdate;
    }
    public class ConversationUpdatePayload
    {
        public List<Source> participants;
    }
    public class SessionControlEvent : ControlEvent
    {
        public SessionConfigurationPayload sessionConfiguration;
    }
    public class SessionConfigurationPayload
    {
        public SessionConfiguration sessionConfiguration;
        public UserRequest userConfiguration;
        public Client clientConfiguration;
        public Capabilities capabilitiesConfiguration;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Continuation continuation;
    }
    public class UserConfigEvent
    {
        public UserRequest userConfiguration;
    }
        
    public class ClientConfigEvent
    {
        public Client clientConfiguration;
    }
    public class CapabilityEvent
    {
        public Capabilities capabilitiesConfiguration;
    }
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
