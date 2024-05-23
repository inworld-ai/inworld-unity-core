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
using System.Linq;

namespace Inworld.Packet
{
    public class Source
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceType type;
        public string name;

        public Source(string targetName = "")
        {
            if (targetName == InworldAI.User.Name)
            {
                name = targetName;
                type = SourceType.PLAYER;
            }
            else
            {
                name = targetName;
                type = targetName == "WORLD" ? SourceType.WORLD : SourceType.AGENT;
            }
        }
    }
    public class Routing
    {
        public Source source;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Source target;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public List<Source> targets;

        public Routing(string character)
        {
            source = new Source(InworldAI.User.Name);
            target = new Source(character);
        }
        public Routing(List<string> characters = null)
        {
            source = new Source(InworldAI.User.Name);

            if (characters == null || characters.Count == 0)
                return;
            // if (characters.Count == 1)
            // {
            //     target = new Source(characters[0]);
            //     return; // TODO(YAN): Always setup targets works. But need to check AEC.
            // }
            targets = new List<Source>();
            foreach (string characterID in characters)
            {
                targets.Add(new Source(characterID));
            }
        }
    }

    public class PacketId
    {
        public string packetId = Guid.NewGuid().ToString();    // Unique.
        public string utteranceId = Guid.NewGuid().ToString(); // Each sentence is an utterance. But can be interpreted as multiple behavior (Text, EmotionChange, Audio, etc)
        public string interactionId = Guid.NewGuid().ToString(); // Lot of sentences included in one interaction.
        public string correlationId; // Used in callback for server packets.
        public string conversationId; // Used in the conversations.

        public override string ToString() => $"I: {interactionId} U: {utteranceId} P: {packetId}";
    }

    public class InworldPacket
    {
        public string timestamp;
        [JsonConverter(typeof(StringEnumConverter))]
        public PacketType type;
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
#region NonSerialized Properties
        [JsonIgnore]
        public virtual string ToJson => JsonConvert.SerializeObject(this); 
        
        [JsonIgnore]
        public SourceType Source => routing?.source?.type ?? SourceType.NONE;
        [JsonIgnore]
        public SourceType Target => routing?.target?.type ?? SourceType.NONE;
        [JsonIgnore]
        public bool IsBroadCast => string.IsNullOrEmpty(routing?.target?.name);
        [JsonIgnore]
        public string SourceName => routing?.source?.name;
        [JsonIgnore]
        public string TargetName => routing?.target?.name;
#endregion
        public bool IsSource(string agentID) => !string.IsNullOrEmpty(agentID) && SourceName == agentID;
        
        public bool IsTarget(string agentID) => !string.IsNullOrEmpty(agentID) && TargetName == agentID;

        public bool Contains(string agentID) => !string.IsNullOrEmpty(agentID) && (routing?.targets?.Any(agent => agent.name == agentID) ?? false);

        public bool IsRelated(string agentID) => IsSource(agentID) || IsTarget(agentID) || Contains(agentID);
    }
}
