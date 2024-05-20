/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Inworld.Packet
{
    [Serializable]
    public class NetworkPacketResponse
    {
        public InworldNetworkPacket result;
        public InworldError error;
    }
    [Serializable]
    public class InworldError
    {
        public int code;
        public string message;
        public List<InworldErrorData> details;
        
        public InworldError(string data)
        {
            code = -1;
            message = data;
            details = new List<InworldErrorData>
            {
                new InworldErrorData
                {
                    errorType = ErrorType.CLIENT_ERROR,
                    reconnectType = ReconnectionType.UNDEFINED,
                    reconnectTime = "",
                    maxRetries = 0
                }
            };
        }
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(message);
        [JsonIgnore]
        public ReconnectionType RetryType  => details[0]?.reconnectType ?? ReconnectionType.UNDEFINED;
        [JsonIgnore]
        public ErrorType ErrorType => details[0]?.errorType ?? ErrorType.UNDEFINED;

    }
    [Serializable]
    public class InworldErrorData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ErrorType errorType;
        [JsonConverter(typeof(StringEnumConverter))]
        public ReconnectionType reconnectType;
        public string reconnectTime;
        public int maxRetries;
    }
    [Obsolete("Use custom deserializer based on NewtonSoft.Json")][Serializable]
    public class InworldNetworkPacket : InworldPacket
    {
        public TextEvent text;
        public ControlEvent control;
        public DataChunk dataChunk;
        public GestureEvent gesture;
        public CustomEvent custom;
        public CancelResponseEvent mutation;
        public EmotionEvent emotion;
        public ActionEvent action;
        public SessionResponseEvent sessionControlResponse;
        [JsonIgnore]
        public InworldPacket Packet
        {
            get
            {
                if (text != null && !string.IsNullOrEmpty(text.text))
                    return new TextPacket(this, text);
                if (control != null && control.action != ControlType.UNKNOWN)
                    return new ControlPacket(this, control);
                if (dataChunk != null && !string.IsNullOrEmpty(dataChunk.chunk) && dataChunk.type == "AUDIO")
                    return new AudioPacket(this, dataChunk);
                if (gesture != null && !string.IsNullOrEmpty(gesture.type))
                    return new GesturePacket(this, gesture);
                if (custom != null && !string.IsNullOrEmpty(custom.name))
                    return new CustomPacket(this, custom);
                if (mutation != null && !string.IsNullOrEmpty(mutation.cancelResponses?.interactionId))
                    return new CancelResponsePacket(this, mutation);
                if (emotion != null && !string.IsNullOrEmpty(emotion.behavior))
                    return new EmotionPacket(this, emotion);
                if (action != null && action.narratedAction != null && !string.IsNullOrEmpty(action.narratedAction.content))
                    return new ActionPacket(this, action);
                if (sessionControlResponse != null && sessionControlResponse.IsValid)
                    return new SessionResponsePacket(this, sessionControlResponse);
                return this;
            }
        }
        [JsonIgnore]
        public PacketType Type
        {
            get
            {
                if (text != null && !string.IsNullOrEmpty(text.text))
                    return PacketType.TEXT;
                if (control != null && control.action != ControlType.UNKNOWN)
                    return PacketType.CONTROL;
                if (dataChunk != null && !string.IsNullOrEmpty(dataChunk.chunk) && dataChunk.type == "AUDIO")
                    return PacketType.AUDIO;
                if (gesture != null && !string.IsNullOrEmpty(gesture.type))
                    return PacketType.GESTURE;
                if (custom != null && !string.IsNullOrEmpty(custom.name))
                    return PacketType.CUSTOM;
                if (mutation != null && !string.IsNullOrEmpty(mutation.cancelResponses?.interactionId))
                    return PacketType.CANCEL_RESPONSE;
                if (emotion != null && !string.IsNullOrEmpty(emotion.behavior))
                    return PacketType.EMOTION;
                if (action != null && action.narratedAction != null && !string.IsNullOrEmpty(action.narratedAction.content))
                    return PacketType.ACTION;
                if (sessionControlResponse != null)
                    return PacketType.SESSION_RESPONSE;
                return PacketType.UNKNOWN;
            }
        }
    }
}
