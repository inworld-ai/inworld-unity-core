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
    public class Token
    {
        public string token;
        public string type;
        public string expirationTime;
        public string sessionId;
        
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(token))
                    return false;
                if (string.IsNullOrEmpty(type))
                    return false;
                return DateTime.UtcNow < InworldDateTime.ToDateTime(expirationTime);
            }
        }
        public SessionControlPacket ToPacket(string gameSessionID = "") => new SessionControlPacket
        {
            timestamp = InworldDateTime.UtcNow,
            type = "SESSION_CONTROL",
            packetId = new PacketId(),
            routing = new Routing(),
            sessionControl = new SessionControlEvent
            {
                sessionConfiguration = new SessionConfiguration(string.IsNullOrEmpty(gameSessionID) ? sessionId : gameSessionID)
            }
        };
    }
    [Serializable]
    public class AccessTokenRequest
    {
        public string api_key;
        public string resource_id;
    }
    [Serializable]
    public class SessionConfiguration
    {
        public string gameSessionId;
        public SessionConfiguration(string sessionID = "")
        {
            gameSessionId = sessionID;
        }
    }
    [Serializable]
    public class SessionControlEvent
    {
        public SessionConfiguration sessionConfiguration;
    }
    
    [Serializable]
    public class SessionControlPacket : InworldPacket
    {
        public SessionControlEvent sessionControl;

        public override string ToJson => JsonUtility.ToJson(this);
    }
}
