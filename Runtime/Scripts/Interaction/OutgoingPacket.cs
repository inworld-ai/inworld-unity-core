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
		public Dictionary<string, string> Targets { get; private set; } // Key: BrainName; Val: AgentID
		public InworldPacket RawPacket { get; protected set; }
        public OutgoingPacket(TextEvent txtToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new TextPacket
            {
	            routing = new Routing(Targets?.Values.ToList()),
	            text = txtToSend
            };
            RawPacket.packetId.correlationId = ID;
        }
        public OutgoingPacket(MutationEvent mutationToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new MutationPacket
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
        public OutgoingPacket(ControlEvent controlToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new ControlPacket
            {
                routing = new Routing(Targets?.Values.ToList()),
                control = controlToSend
            };
        }
        public OutgoingPacket(DataChunk chunkToSend, Dictionary<string, string> characterTable = null)
        {
	        ID = Guid.NewGuid().ToString();
            Targets = characterTable;
            RawPacket = new AudioPacket()
            {
                routing = new Routing(characterTable?.Values.ToList()),
                dataChunk = chunkToSend
            };
        }

        public bool IsCharacterRegistered => !Targets.Values.Any(string.IsNullOrEmpty);
        
		public bool Contains(InworldPacket packet)
		{
			throw new NotImplementedException();
		}
		public void Add(InworldPacket packet)
		{
			throw new NotImplementedException();
		}
		public void Cancel(bool isHardCancelling = true)
		{
			throw new NotImplementedException();
		}
		public void OnDequeue()
		{
			_UpdateSessionInfo();
			_ComposePacket();
		}
		
		/// <summary>
		/// Update the characters in this conversation with updated ID.
		/// </summary>
		/// <returns>The brain name of the characters not found in the current session.</returns>
		void _UpdateSessionInfo()
		{
			foreach (string key in Targets.Keys.ToList())
			{
				if (InworldController.Client.LiveSessionData.TryGetValue(key, out InworldCharacterData value))
					Targets[key] = value.agentId;
				else if (InworldAI.IsDebugMode)
					InworldAI.LogWarning($"{key} is not in the current session.");
			}
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
