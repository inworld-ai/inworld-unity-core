/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using Inworld.Packet;
using TMPro;
using UnityEngine;

namespace Inworld.Sample
{
    /// <summary>
    /// A simple sample for only displaying subtitle
    /// </summary>
    public class Subtitle : MonoBehaviour
    {
        [SerializeField] TMP_Text m_Subtitle;
        
        string m_CurrentEmotion;
        string m_CurrentContent;
        void OnEnable()
        {
            InworldController.CharacterHandler.OnCharacterListJoined += OnCharacterJoined;
            InworldController.CharacterHandler.OnCharacterListLeft += OnCharacterLeft;
        }
        void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.CharacterHandler.OnCharacterListJoined -= OnCharacterJoined;
            InworldController.CharacterHandler.OnCharacterListLeft -= OnCharacterLeft;
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
        protected virtual void OnInteraction(InworldPacket packet)
        {
            if (!m_Subtitle)
                return;
            if (!(packet is TextPacket playerPacket))
                return;
            switch (packet.routing.source.type.ToUpper())
            {
                case "PLAYER":
                    m_Subtitle.text = $"{InworldAI.User.Name}: {playerPacket.text.text}";
                    break;
                case "AGENT":
                    if (!InworldController.Client.LiveSessionData.TryGetValue(packet.routing.source.name, out InworldCharacterData character))
                        return;
                    m_Subtitle.text = $"{character.givenName}: {playerPacket.text.text}";
                    break;
            }
        }
    }
}
