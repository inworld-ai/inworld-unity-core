﻿/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;
namespace Inworld.Packet
{
    [Serializable]
    public class Source
    {
        public string type;
        public string name;
    }
    [Serializable]
    public class Routing
    {
        public Source source;
        public Source target;
        public List<Source> targets;
        public Routing()
        {
            source = new Source();
            target = new Source();
            targets = new List<Source>();
        }
        public Routing(string id = "", List<string> characters = null)
        {
            source = new Source
            {
                name = "player",
                type = "PLAYER",
            };
            target = new Source
            {
                name = id,
                type = "AGENT",
            };
            targets = new List<Source>();
            if (characters != null)
            {
                foreach (string characterID in characters)
                {
                    targets.Add(new Source
                    {
                        name = characterID,
                        type = "AGENT"
                    });
                }
            }
            else if (!string.IsNullOrEmpty(id))
            {
                targets.Add(new Source
                {
                    name = id,
                    type = "AGENT"
                });
            }
        }
    }

    [Serializable]
    public class PacketId
    {
        public string packetId = Guid.NewGuid().ToString();    // Unique.
        public string utteranceId = Guid.NewGuid().ToString(); // Each sentence is an utterance. But can be interpreted as multiple behavior (Text, EmotionChange, Audio, etc)
        public string interactionId = Guid.NewGuid().ToString(); // Lot of sentences included in one interaction.
        public string correlationId; // Used in future.

        public override string ToString() => $"I: {interactionId} U: {utteranceId} P: {packetId}";
        
        [Obsolete] public PacketStatus Status { get; set; }
    }

    [Serializable]
    public class InworldPacket
    {
        public string timestamp;
        public string type;
        public PacketId packetId;
        public Routing routing;

        public InworldPacket()
        {
            timestamp = InworldDateTime.UtcNow;
            packetId = new PacketId();
            routing = new Routing();
        }
        public InworldPacket(InworldPacket rhs)
        {
            timestamp = rhs.timestamp;
            packetId = rhs.packetId;
            routing = rhs.routing;
            type = rhs.type;
        }
    }
}
