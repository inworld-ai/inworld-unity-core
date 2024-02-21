/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
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
                type = id == "WORLD" ? id : "AGENT",
            };
            if (characters == null)
                return;
            foreach (string characterID in characters)
            {
                targets = new List<Source>
                {
                    new Source
                    {
                        name = characterID,
                        type = "AGENT"
                    }
                };
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
        public SourceType Source => Enum.TryParse(routing?.source?.type, true, out SourceType result) ? result : SourceType.NONE;
        
        public SourceType Target => Enum.TryParse(routing?.target?.type, true, out SourceType result) ? result : SourceType.NONE;

        public bool IsBroadCast => string.IsNullOrEmpty(routing?.target?.name);

        public string SourceName => routing?.source?.name;
        
        public string TargetName => routing?.target?.name;
        
        public bool IsSource(string agentID) => !string.IsNullOrEmpty(agentID) && SourceName == agentID;
        
        public bool IsTarget(string agentID) => !string.IsNullOrEmpty(agentID) && TargetName == agentID;

        public bool Contains(string agentID) => !string.IsNullOrEmpty(agentID) && (routing?.targets?.Any(agent => agent.name == agentID) ?? false);

        public bool IsRelated(string agentID) => IsSource(agentID) || IsTarget(agentID) || Contains(agentID);
    }
}
