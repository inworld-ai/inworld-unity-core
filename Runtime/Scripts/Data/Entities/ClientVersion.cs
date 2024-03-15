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
    public class Client
    {
        public string id;
        public string version;
        public string description;

        public ClientConfigPacket ToPacket => new ClientConfigPacket
        {
            timestamp = InworldDateTime.UtcNow,
            type = "SESSION_CONTROL",
            packetId = new PacketId(),
            routing = new Routing(),
            sessionControl = new ClientConfigEvent
            {
                clientConfiguration = this
            }
        };
        public override string ToString() => $"{id}: {version} {description}";
    }
    [Serializable]
    public class ReleaseData
    {
        public PackageData[] package;
    }

    [Serializable]
    public class PackageData
    {
        public string published_at;
        public string tag_name;
    }

    [Serializable]
    public class ClientConfigEvent
    {
        public Client clientConfiguration;
    }
    
    [Serializable]
    public class ClientConfigPacket : InworldPacket
    {
        public ClientConfigEvent sessionControl;
        public override string ToJson => JsonUtility.ToJson(this);
    }
}
