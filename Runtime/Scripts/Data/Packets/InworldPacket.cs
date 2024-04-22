/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inworld.Packet
{
    [Serializable]
    public class Source
    {
        public string type;
        public string name;

        public Source(string targetName)
        {
            if (targetName == "WORLD")
            {
                type = SourceType.WORLD.ToString();
            }
            else if (targetName == InworldAI.User.Name)
            {
                name = targetName;
                type = SourceType.PLAYER.ToString();
            }
            else
            {
                name = targetName;
                type = SourceType.AGENT.ToString();
            }
        }
    }
    [Serializable]
    public class Routing
    {
        public Source source;
        public Source target;
        public List<Source> targets;

        public Routing()
        {
            source = new Source(InworldAI.User.Name);
        }
        public Routing(string character)
        {
            source = new Source(InworldAI.User.Name);
            target = new Source(character);
        }
        public Routing(List<string> characters)
        {
            source = new Source(InworldAI.User.Name);
            targets = new List<Source>();
            foreach (string characterID in characters)
            {
                targets.Add(new Source(characterID));
            }
        }
    }

    [Serializable]
    public class PacketId
    {
        public string packetId = Guid.NewGuid().ToString();    // Unique.
        public string utteranceId = Guid.NewGuid().ToString(); // Each sentence is an utterance. But can be interpreted as multiple behavior (Text, EmotionChange, Audio, etc)
        public string interactionId = Guid.NewGuid().ToString(); // Lot of sentences included in one interaction.
        public string correlationId; // Used in callback for server packets.
        public string conversationId; // Used in group conversation.

        public override string ToString() => $"I: {interactionId} U: {utteranceId} P: {packetId} Callback: {string.IsNullOrEmpty(conversationId)} Conversation: {string.IsNullOrEmpty(conversationId)}";
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
        public virtual string ToJson => JsonUtility.ToJson(this); 
        public SourceType Source => Enum.TryParse(routing?.source?.type, true, out SourceType result) ? result : SourceType.NONE;
        
        public SourceType Target => Enum.TryParse(routing?.target?.type, true, out SourceType result) ? result : SourceType.NONE;

        public bool IsBroadCast => string.IsNullOrEmpty(routing?.target?.name);

        public string SourceName => routing?.source?.name;
        
        public string TargetName => routing?.target?.name;

        public string TargetAgentID
        {
            get
            {
                string agentID = TargetName;
                if (!string.IsNullOrEmpty(agentID))
                    return agentID;
                string conversationID = packetId?.conversationId;
                if (!string.IsNullOrEmpty(conversationID))
                    return packetId.conversationId;
                return "";
            }
        }
        
        public bool IsSource(string agentID) => !string.IsNullOrEmpty(agentID) && SourceName == agentID;
        
        public bool IsTarget(string agentID) => !string.IsNullOrEmpty(agentID) && TargetName == agentID;

        public bool Contains(string agentID) => !string.IsNullOrEmpty(agentID) && (routing?.targets?.Any(agent => agent.name == agentID) ?? false);

        public bool IsRelated(string agentID) => IsSource(agentID) || IsTarget(agentID) || Contains(agentID);
    }
}
