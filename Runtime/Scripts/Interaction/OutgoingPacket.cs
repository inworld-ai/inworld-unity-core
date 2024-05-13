﻿/*************************************************************************************************
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
		public Dictionary<string, string> Targets { get; private set; } // Key: BrainName; Val: AgentID
		public InworldPacket RawPacket { get; protected set; }
        public OutgoingPacket(TextEvent txtToSend)
        {
	        LiveInfo liveInfo = InworldController.Client.Current;
	        ID = Guid.NewGuid().ToString();
	        RawPacket = new TextPacket
	        {
		        text = txtToSend
	        };
	        if (liveInfo.Character == null)
		        RawPacket.packetId.conversationId = liveInfo.Conversation.ID;
	        else
	        {
		        Targets = new Dictionary<string, string>
		        {
			        [liveInfo.Character.brainName] = liveInfo.Character.agentId
		        };
		        RawPacket.routing = new Routing(liveInfo.Character.agentId);
	        }
            RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(ActionEvent narrativeActionToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
	        Targets = characterTable;
	        RawPacket = new ActionPacket()
	        {
		        routing = new Routing(Targets?.Values.ToList()),
		        action = narrativeActionToSend
	        };
	        RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(CancelResponseEvent mutationToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new CancelResponsePacket
            {
                routing = new Routing(Targets?.Values.ToList()),
                mutation = mutationToSend
            };
        }
        public OutgoingPacket(CustomEvent triggerToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new CustomPacket
            {
                routing = new Routing(Targets?.Values.ToList()),
                custom = triggerToSend
            };
        }
        public OutgoingPacket(ControlEvent controlToSend)
        {
	        LiveInfo liveInfo = InworldController.Client.Current;
	        ID = Guid.NewGuid().ToString();
	        RawPacket = new ControlPacket
	        {
		        control = controlToSend
	        };
	        if (liveInfo.IsConversation)
	        {
		        RawPacket.packetId.conversationId = liveInfo.Conversation.ID;
	        }
	        else
	        {
		        Targets = new Dictionary<string, string>
		        {
			        [liveInfo.Character.brainName] = liveInfo.Character.agentId
		        };
		        RawPacket.routing = new Routing(liveInfo.Character.agentId);
	        }
        }
        public OutgoingPacket(DataChunk chunkToSend)
        {
	        LiveInfo liveInfo = InworldController.Client.Current;
	        ID = Guid.NewGuid().ToString();
	        RawPacket = new AudioPacket
	        {
		        dataChunk = chunkToSend
	        };
	        if (liveInfo.IsConversation)
	        {
		        RawPacket.packetId.conversationId = liveInfo.Conversation.ID;
	        }
	        else
	        {
		        Targets = new Dictionary<string, string>
		        {
			        [liveInfo.Character.brainName] = liveInfo.Character.agentId
		        };
		        RawPacket.routing = new Routing("");
	        }
        }

        public bool IsCharacterRegistered => !Targets.Values.Any(string.IsNullOrEmpty);
        
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
		public bool OnDequeue()
		{
			if (_UpdateSessionInfo())
			{
				_ComposePacket();
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Update the characters in this conversation with updated ID.
		/// </summary>
		/// <returns>The brain name of the characters not found in the current session.</returns>
		bool _UpdateSessionInfo()
		{
			if (Targets == null)
				return false;
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
			return Targets.Count > 0 && !Targets.Values.Any(string.IsNullOrEmpty);
		}

		void _ComposePacket()
		{
			List<string> agentIDs = Targets.Values.Where(c => !string.IsNullOrEmpty(c)).ToList();
			if (RawPacket == null)
				return;
			RawPacket.routing = new Routing(agentIDs);
		}
	}
}
