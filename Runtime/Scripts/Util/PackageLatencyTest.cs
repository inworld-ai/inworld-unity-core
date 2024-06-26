/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using UnityEngine;

namespace Inworld.Sample
{
    public class PackageLatencyTest : MonoBehaviour
    {
        [SerializeField] PacketType m_PacketType = PacketType.TEXT;
        [SerializeField] bool m_IsEnabled;
        
        bool IsFromPlayer(InworldPacket packet) => packet.Source == SourceType.PLAYER;
        bool m_LastPacketIsFromPlayer;
        float m_PlayerTime;
        float m_ServerTime;
        void OnEnable()
        {
            InworldController.CharacterHandler.Event.onCharacterListJoined.AddListener(OnCharacterJoined);
            InworldController.CharacterHandler.Event.onCharacterListLeft.AddListener(OnCharacterLeft);
        }

        void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.CharacterHandler.Event.onCharacterListJoined.RemoveListener(OnCharacterJoined);
            InworldController.CharacterHandler.Event.onCharacterListLeft.RemoveListener(OnCharacterLeft);
        }
        protected virtual void OnCharacterJoined(InworldCharacter character)
        {
            // YAN: Clear existing event listener to avoid adding multiple times.
            character.Event.onPacketReceived.RemoveListener(OnInteraction); 
            character.Event.onPacketReceived.AddListener(OnInteraction);
        }

        protected virtual void OnCharacterLeft(InworldCharacter character)
        {
            character.Event.onPacketReceived.RemoveListener(OnInteraction); 
        }
        void OnInteraction(InworldPacket incomingPacket)
        {
            if (incomingPacket.type== m_PacketType && IsFromPlayer(incomingPacket))
            {
                m_LastPacketIsFromPlayer = true;
                m_PlayerTime = Time.time;
            }
            else if (incomingPacket.type == m_PacketType)
            {
                if (m_LastPacketIsFromPlayer && m_IsEnabled)
                    InworldAI.Log($"Package Latency: {Time.time - m_PlayerTime}");
                m_LastPacketIsFromPlayer = false;
            }
        }
    }
}
