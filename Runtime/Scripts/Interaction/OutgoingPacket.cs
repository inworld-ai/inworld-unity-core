/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using UnityEngine;
using Inworld.Packet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Inworld.Interactions
{
	public class OutgoingPacket : IContainable
	{
		public string ID { get; set; }	// YAN: This ID is correlation ID.
		public DateTime RecentTime { get; set; }
		public bool IsEmpty { get; }
		public string BrainName { get; set; } // YAN: The character ID in the browser.
		public string AgentID { get; set; } 
		public InworldPacket RawPacket { get; protected set; }
		List<string> m_BrainNames = new List<string>();
        public OutgoingPacket(TextEvent txtToSend, LiveInfo liveInfo)
        {
	        ID = Guid.NewGuid().ToString();
	        if (liveInfo.IsConversation)
	        {
		        RawPacket = new TextPacket
		        {
			        routing = new Routing("WORLD"),
			        text = txtToSend
		        };
		        RawPacket.packetId.conversationId = liveInfo.ConversationID;
	        }
	        else
	        {
		        BrainName = liveInfo.CurrentBrainName;
		        RawPacket = new TextPacket
		        {
			        routing = new Routing(liveInfo.CurrentAgentID),
			        text = txtToSend
		        };
	        }
            RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(ActionEvent narrativeActionToSend, LiveInfo liveInfo) 
        {
	        ID = Guid.NewGuid().ToString();
	        if (liveInfo.IsConversation)
	        {
		        RawPacket = new ActionPacket
		        {
			        routing = new Routing("WORLD"),
			        action = narrativeActionToSend
		        };
		        RawPacket.packetId.conversationId = liveInfo.ConversationID;
	        }
	        else
	        {
		        BrainName = liveInfo.CurrentBrainName;
		        RawPacket = new ActionPacket
		        {
			        routing = new Routing(liveInfo.CurrentAgentID),
			        action = narrativeActionToSend
		        };
	        }
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(CancelResponseEvent mutationToSend, LiveInfo liveInfo) 
        {
	        ID = Guid.NewGuid().ToString();
	        if (liveInfo.IsConversation)
	        {
		        RawPacket = new CancelResponsePacket
		        {
			        routing = new Routing("WORLD"),
			        mutation = mutationToSend
		        };
		        RawPacket.packetId.conversationId = liveInfo.ConversationID;
	        }
	        else
	        {
		        BrainName = liveInfo.CurrentBrainName;
		        RawPacket = new CancelResponsePacket
		        {
			        routing = new Routing(liveInfo.CurrentAgentID),
			        mutation = mutationToSend
		        };
	        }
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(RegenerateResponseEvent mutationToSend, LiveInfo liveInfo) 
        {
	        ID = Guid.NewGuid().ToString();
	        if (liveInfo.IsConversation)
	        {
		        RawPacket = new RegenerateResponsePacket
		        {
			        routing = new Routing("WORLD"),
			        mutation = mutationToSend
		        };
		        RawPacket.packetId.conversationId = liveInfo.ConversationID;
	        }
	        else
	        {
		        BrainName = liveInfo.CurrentBrainName;
		        RawPacket = new RegenerateResponsePacket
		        {
			        routing = new Routing(liveInfo.CurrentAgentID),
			        mutation = mutationToSend
		        };
	        }
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(CustomEvent triggerToSend, LiveInfo liveInfo)
        {
	        ID = Guid.NewGuid().ToString();
	        BrainName = string.IsNullOrEmpty(liveInfo.CurrentBrainName) ? "WORLD" : liveInfo.CurrentBrainName;
	        RawPacket = new CustomPacket
	        {
		        routing = new Routing(string.IsNullOrEmpty(liveInfo.CurrentAgentID) ? "WORLD" : liveInfo.CurrentAgentID),
		        custom = triggerToSend
	        };
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(string conversationID, List<string> brainNames)
        {
	        ID = Guid.NewGuid().ToString();
	        m_BrainNames = brainNames;
	        RawPacket = new ConversationPacket
	        {
		        routing = new Routing("WORLD"),
		        control = new ConversationEvent
		        {
			        action = ControlType.CONVERSATION_UPDATE.ToString()
		        }
	        };
	        RawPacket.packetId.conversationId = conversationID;
        }
        public OutgoingPacket(ControlEvent controlToSend, LiveInfo liveInfo)
        {
	        ID = Guid.NewGuid().ToString();
	        if (liveInfo.IsConversation)
	        {
		        RawPacket = new ControlPacket
		        {
			        routing = new Routing("WORLD"),
			        control = controlToSend
		        };
		        RawPacket.packetId.conversationId = liveInfo.ConversationID;
	        }
	        else
	        {
		        BrainName = liveInfo.CurrentBrainName;
		        RawPacket = new ControlPacket
		        {
			        routing = new Routing(liveInfo.CurrentAgentID),
			        control = controlToSend
		        };
	        }
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(DataChunk chunkToSend, string brainName, string agentID = "")
        {
	        ID = Guid.NewGuid().ToString();
	        BrainName = brainName;
            RawPacket = new AudioPacket
            {
                routing = new Routing(agentID),
                dataChunk = chunkToSend
            };
        }
        public bool IsConversation => !string.IsNullOrEmpty(RawPacket?.packetId?.conversationId);
		public bool Contains(InworldPacket packet)
		{
			return RawPacket.packetId == packet.packetId;
		}
		public void Add(InworldPacket packet)
		{
			throw new NotImplementedException();
		}
		public void Cancel(bool isHardCancelling = true)
		{
			throw new NotImplementedException();
		}
		public bool OnDequeue() => _UpdateSessionInfo();
	
		/// <summary>
		/// Update the characters in this conversation with updated ID.
		/// </summary>
		/// <returns>The brain name of the characters not found in the current session.</returns>
		bool _UpdateSessionInfo()
		{
			if (m_BrainNames.Count != 0 && RawPacket is ConversationPacket convoPacket && convoPacket.IsConversation)
			{
				List<Source> agentIDs = new List<Source>();
				foreach (string brainName in m_BrainNames)
				{
					if (InworldController.Client.LiveSessionData.TryGetValue(brainName, out InworldCharacterData val))
					{
						agentIDs.Add(new Source(val.agentId));
					}
				}
				convoPacket.InstallPayload(agentIDs);
				return agentIDs.Count > 0;
			}
			// return true;
			if (string.IsNullOrEmpty(BrainName))
			{
				if (IsConversation)
				{
					Debug.Log("Send to Convo");
					return true;
				}
				if (InworldAI.IsDebugMode)
					InworldAI.LogWarning($"Invalid packet with empty character.");
				return false;
			}
			if (InworldController.Client.LiveSessionData.TryGetValue(BrainName, out InworldCharacterData value))
			{
				AgentID = value.agentId;
				RawPacket.routing = new Routing(AgentID);
				return !string.IsNullOrEmpty(AgentID);
			}
			if (InworldAI.IsDebugMode)
				InworldAI.LogWarning($"{BrainName} is not in the current session.");
			return false;
		}
	}
}
