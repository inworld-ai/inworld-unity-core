/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Inworld.Packet
{
    [Serializable]
    public class NetworkPacketResponse
    {
        public InworldNetworkPacket result;
    }
    [Serializable]
    public class InworldNetworkPacket : InworldPacket
    {
        public TextEvent text;
        public ControlEvent control;
        public DataChunk dataChunk;
        public GestureEvent gesture;
        public CustomEvent custom;
        public MutationEvent mutation;
        public EmotionEvent emotion;
        public ActionEvent action;
        public SessionResponseEvent sessionControlResponse;
        public InworldPacket Packet
        {
            get
            {
                if (text != null && !string.IsNullOrEmpty(text.text))
                    return new TextPacket(this, text);
                if (control != null && !string.IsNullOrEmpty(control.action))
                    return new ControlPacket(this, control);
                if (dataChunk != null && !string.IsNullOrEmpty(dataChunk.chunk) && dataChunk.type == "AUDIO")
                    return new AudioPacket(this, dataChunk);
                if (gesture != null && !string.IsNullOrEmpty(gesture.type))
                    return new GesturePacket(this, gesture);
                if (custom != null && !string.IsNullOrEmpty(custom.name))
                    return new CustomPacket(this, custom);
                if (mutation != null && !string.IsNullOrEmpty(mutation.cancelResponses?.interactionId))
                    return new MutationPacket(this, mutation);
                if (emotion != null && !string.IsNullOrEmpty(emotion.behavior))
                    return new EmotionPacket(this, emotion);
                if (action != null && action.narratedAction != null && !string.IsNullOrEmpty(action.narratedAction.content))
                    return new ActionPacket(this, action);
                if (sessionControlResponse != null)
                    return new SessionResponsePacket(this, sessionControlResponse);
                return this;
            }
        }
        public PacketType Type
        {
            get
            {
                if (text != null && !string.IsNullOrEmpty(text.text))
                    return PacketType.TEXT;
                if (control != null && !string.IsNullOrEmpty(control.action))
                    return PacketType.CONTROL;
                if (dataChunk != null && !string.IsNullOrEmpty(dataChunk.chunk) && dataChunk.type == "AUDIO")
                    return PacketType.AUDIO;
                if (gesture != null && !string.IsNullOrEmpty(gesture.type))
                    return PacketType.GESTURE;
                if (custom != null && !string.IsNullOrEmpty(custom.name))
                    return PacketType.CUSTOM;
                if (mutation != null && !string.IsNullOrEmpty(mutation.cancelResponses?.interactionId))
                    return PacketType.CANCEL_RESPONSE;
                if (emotion != null && !string.IsNullOrEmpty(emotion.behavior))
                    return PacketType.EMOTION;
                if (action != null && action.narratedAction != null && !string.IsNullOrEmpty(action.narratedAction.content))
                    return PacketType.ACTION;
                if (sessionControlResponse != null)
                    return PacketType.SESSION_RESPONSE;
                return PacketType.UNKNOWN;
            }
        }
    }

    [Serializable]
    public class OutgoingPacketData
    {
        public string correlationID;
        public Dictionary<string, string> charactersToReceive; // Key: BrainName. Val: AgentID.
        public List<InworldPacket> outgoingPackets;
        public bool hasSent = false;

        public OutgoingPacketData(TextEvent txtToSend, Dictionary<string, string> characterTable = null)
        {
            correlationID = Guid.NewGuid().ToString();
            charactersToReceive = characterTable;
            outgoingPackets = new List<InworldPacket>();
            InworldPacket packet = new TextPacket
            {
                routing = new Routing(charactersToReceive?.Values.ToList()),
                text = txtToSend
            };
            outgoingPackets.Add(packet);
        }
        public OutgoingPacketData(MutationEvent mutationToSend, Dictionary<string, string> characterTable = null)
        {
            correlationID = Guid.NewGuid().ToString();
            charactersToReceive = characterTable;
            outgoingPackets = new List<InworldPacket>();
            InworldPacket packet = new MutationPacket()
            {
                routing = new Routing(charactersToReceive?.Values.ToList()),
                mutation = mutationToSend
            };
            outgoingPackets.Add(packet);
        }
        public OutgoingPacketData(CustomEvent triggerToSend, Dictionary<string, string> characterTable = null)
        {
            correlationID = Guid.NewGuid().ToString();
            charactersToReceive = characterTable;
            outgoingPackets = new List<InworldPacket>();
            InworldPacket packet = new CustomPacket()
            {
                routing = new Routing(charactersToReceive?.Values.ToList()),
                custom = triggerToSend
            };
            outgoingPackets.Add(packet);
        }
        public OutgoingPacketData(ControlEvent controlToSend, Dictionary<string, string> characterTable = null)
        {
            correlationID = Guid.NewGuid().ToString();
            charactersToReceive = characterTable;
            outgoingPackets = new List<InworldPacket>();
            InworldPacket packet = new ControlPacket()
            {
                routing = new Routing(charactersToReceive?.Values.ToList()),
                control = controlToSend
            };
            outgoingPackets.Add(packet);
        }
        public OutgoingPacketData(DataChunk chunkToSend, Dictionary<string, string> characterTable = null)
        {
            correlationID = Guid.NewGuid().ToString();
            charactersToReceive = characterTable;
            outgoingPackets = new List<InworldPacket>();
            InworldPacket packet = new AudioPacket()
            {
                routing = new Routing(characterTable?.Values.ToList()),
                dataChunk = chunkToSend
            };
            outgoingPackets.Add(packet);
        }
        public bool IsCharacterRegistered => !charactersToReceive.Values.Any(string.IsNullOrEmpty);

        /// <summary>
        /// Update the characters in this conversation with updated ID.
        /// </summary>
        /// <returns>The brain name of the characters not found in the current session.</returns>
        public List<string> UpdateSessionInfo()
        {
            List<string> result = new List<string>();
            foreach (string key in charactersToReceive.Keys.ToList())
            {
                if (InworldController.Client.LiveSessionData.TryGetValue(key, out InworldCharacterData value))
                    charactersToReceive[key] = value.agentId;
                else
                    result.Add(key);
            }
            return result;
        }

        public List<InworldPacket> ComposePacket()
        {
            List<string> agentIDs = charactersToReceive.Values.Where(c => !string.IsNullOrEmpty(c)).ToList();
            // if (agentIDs.Count == 0)
            //     return null;
            foreach (InworldPacket packet in outgoingPackets)
            {
                packet.packetId.correlationId = correlationID;
                packet.routing = new Routing(agentIDs);
            }
            return outgoingPackets;
        }
    }
}
