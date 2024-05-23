/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using Inworld.Interactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

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
        /// <summary>
        /// Key/Value Pair for targets to send.
        /// Key is character's full name.
        /// Value is its agent ID [Nullable] (We'll fetch it in the UpdateSessionInfo)
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Targets { get; set; } = new Dictionary<string, string>();
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

        protected virtual void PreProcess()
        {
            // ReSharper disable Unity.PerformanceCriticalCodeInvocation
            // because InworldController.Client's GetComponent would not be called mostly.
            packetId.correlationId = Guid.NewGuid().ToString();
            LiveInfo liveInfo = InworldController.Client.Current;
            if (liveInfo.Character == null)
                packetId.conversationId = liveInfo.Conversation.ID;
            else
            {
                Targets = new Dictionary<string, string>
                {
                    [liveInfo.Character.brainName] = liveInfo.Character.agentId
                };
                routing = new Routing(liveInfo.Character.agentId);
            }
        }
        // YAN: Only for packets that needs to explicitly set multiple targets (Like start conversation).
        //		Usually for conversation packets, do not need to call this.
        protected virtual void PreProcess(Dictionary<string, string> targets)
        {
            packetId.correlationId = Guid.NewGuid().ToString();
            LiveInfo liveInfo = InworldController.Client.Current;
            packetId.conversationId = liveInfo.Conversation.ID;
            Targets = targets;
            routing = new Routing(targets.Values.ToList());
        }
        /// <summary>
        /// Originally OnDequeue in OutgoingPacket.
        /// Always call it before send to fetch the agent ID. 
        /// </summary>
        public void PrepareToSend()
        {
            if (UpdateSessionInfo())
                UpdateRouting();
        }
        /// <summary>
        /// Update the characters in this conversation with updated ID.
        /// </summary>
        /// <returns>The brain name of the characters not found in the current session.</returns>
        protected virtual bool UpdateSessionInfo()
        {
            if (Targets == null || Targets.Count == 0)
            {
                if (!InworldController.Client.Current.IsConversation)
                    return false;
                routing = new Routing();
                return true;
            }
            foreach (string key in Targets.Keys.ToList().Where(key => !string.IsNullOrEmpty(key)))
            {
                if (InworldController.Client.LiveSessionData.TryGetValue(key, out InworldCharacterData value))
                    Targets[key] = value.agentId;
                else 
                {
                    if (InworldAI.IsDebugMode)
                        InworldAI.LogWarning($"{key} is not in the current session.");
                }
            }
            return Targets.Count > 0 && Targets.Values.Any(id => !string.IsNullOrEmpty(id));
        }
 
        protected virtual void UpdateRouting()
        {
            if (Targets == null || Targets.Count == 0)
            {
                // Conversation
                packetId.conversationId = string.IsNullOrEmpty(packetId.conversationId) 
                    ? InworldController.CharacterHandler.ConversationID 
                    : packetId.conversationId;
                return; 
            }
            List<string> agentIDs = Targets.Values.Where(c => !string.IsNullOrEmpty(c)).ToList();
            routing = new Routing(agentIDs);
        }
        public bool IsSource(string agentID) => !string.IsNullOrEmpty(agentID) && SourceName == agentID;
        
        public bool IsTarget(string agentID) => !string.IsNullOrEmpty(agentID) && TargetName == agentID;

        public bool Contains(string agentID) => !string.IsNullOrEmpty(agentID) && (routing?.targets?.Any(agent => agent.name == agentID) ?? false);

        public bool IsRelated(string agentID) => IsSource(agentID) || IsTarget(agentID) || Contains(agentID);
    }
}
