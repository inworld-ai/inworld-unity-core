﻿/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Inworld.Interactions
{
    public class Utterance : IContainable
    {
        readonly Dictionary<string, InworldPacket> m_Packets;
        public string ID { get; set; }
        public DateTime RecentTime { get; set; }
        public List<InworldPacket> Packets => m_Packets.Values.ToList();
        public bool IsEmpty => m_Packets == null || m_Packets.Count == 0;
        public bool Contains(InworldPacket packet) => ID == packet?.packetId?.utteranceId;
        public Utterance(InworldPacket packet)
        {
            ID = packet?.packetId?.utteranceId;
            RecentTime = InworldDateTime.ToDateTime(packet?.timestamp);
            m_Packets = new Dictionary<string, InworldPacket>();
            string packetID = packet?.packetId?.packetId;
            if (!string.IsNullOrEmpty(packetID))
                m_Packets[packetID] = packet;
        }
        public void Add(InworldPacket packet)
        {
            if (packet == null || packet.packetId == null)
                return;
            if (ID != packet.packetId.utteranceId)
                return;
            DateTime packetTime = InworldDateTime.ToDateTime(packet.timestamp);
            RecentTime = packetTime > RecentTime ? packetTime : RecentTime;
            m_Packets[packet.packetId.packetId] = packet;
        }
        public bool IsPlayable()
        {
            foreach (InworldPacket p in m_Packets.Values)
            {
                if (p.Source == SourceType.PLAYER)
                    return false;
                if (p is TextPacket || p is AudioPacket)
                    return true;
            }
            return false;
        }
        public bool ContainsTextAndAudio()
        {
            bool foundAudio = false, foundText = false;
            foreach (InworldPacket p in m_Packets.Values)
            {
                switch (p)
                {
                    case TextPacket textPacket:
                        foundText = true;
                        break;
                    case AudioPacket audioPacket:
                        foundAudio = true;
                        break;
                }
            }
            return foundAudio && foundText;
        }
        public float GetTextSpeed()
        {
            foreach (InworldPacket p in m_Packets.Values)
            {
                if (p is TextPacket textPacket)
                    return textPacket.text.text.Length;
                if (p is ActionPacket actionPacket)
                    return actionPacket.action.narratedAction.content.Length;
            }
            return 0;
        }
        public AudioClip GetAudioClip()
        {
            foreach (InworldPacket p in m_Packets.Values)
            {
                if (p is AudioPacket audioPacket)
                    return audioPacket.Clip;
            }
            return null;
        }
        public void Cancel(bool isHardCancelling = true)
        {
            m_Packets.Clear();
        }
        public virtual bool OnDequeue()
        {
            // YAN: Inherit your logic here in case you want to do specific stuff when dequeuing.
            return true;
        }
    }
}
