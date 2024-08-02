/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Inworld
{
    public class CharacterHandler : MonoBehaviour
    {
        InworldCharacter m_CurrentCharacter;
        string m_ConversationID;
        [SerializeField] ConversationEvents m_Events;
        
        // The Character List only lists the interactable characters. 
        // Although InworldCharacter also has InworldCharacterData, its agentID won't be always updated. Please check m_LiveSession
        // and Call RegisterLiveSession if outdated.
        protected readonly List<InworldCharacter> m_CharacterList = new List<InworldCharacter>();

        /// <summary>
        /// Gets the conversation Events.
        /// </summary>
        public ConversationEvents Event => m_Events;

        /// <summary>
        /// Gets the conversation ID.
        /// It'll create one if not exist.
        /// </summary>
        public string ConversationID
        {
            get
            {
                if (string.IsNullOrEmpty(m_ConversationID))
                    StartNewConversation();
                return m_ConversationID;
            }
        }
        
        /// <summary>
        /// Return if any character is speaking.
        /// </summary>
        public virtual bool IsAnyCharacterSpeaking => m_CharacterList.Any(c => c.IsSpeaking);

        /// <summary>
        /// Gets the list of all the current character's brain name in the conversation.
        /// </summary>
        public List<string> CurrentCharacterNames => m_CharacterList.Select(a => a.BrainName).ToList();

        /// <summary>
        /// Gets/Sets the current interacting character. Mainly for backwards compatibility.
        /// Although you're allowed to talk to multiple characters, sometimes you need to nominate the character (And don't unregister others).
        ///
        /// This parameter will overwrite the group chat CurrentCharacters.
        /// </summary>
        public InworldCharacter CurrentCharacter
        {
            get => m_CurrentCharacter;
            set
            {
                string oldBrainName = m_CurrentCharacter ? m_CurrentCharacter.BrainName : "";
                string newBrainName = value ? value.BrainName : "";
                if (oldBrainName == newBrainName)
                    return;
                if (m_CurrentCharacter)
                    m_CurrentCharacter.Event.onCharacterDeselected.Invoke(m_CurrentCharacter.BrainName);
                m_CurrentCharacter = value;
                if (m_CurrentCharacter)
                    m_CurrentCharacter.Event.onCharacterSelected.Invoke(m_CurrentCharacter.BrainName);
            }
        }
        /// <summary>
        /// Gets the current interacting characters in the group.
        /// If set, it'll also start audio sampling if `ManualAudioHandling` is false, and invoke the event OnCharacterChanged
        /// </summary>
        public List<InworldCharacter> CurrentCharacters => m_CharacterList;

        /// <summary>
        ///     Get the current Character Selecting Method. By default it's manual.
        /// </summary>
        public virtual CharSelectingMethod SelectingMethod { get; set; } = CharSelectingMethod.Manual;

        /// <summary>
        /// Start a new conversation.
        /// In Unity SDK by default, we only use one single conversation for handling all the characters.
        /// But it's able to handle multiple conversations by developers.
        ///
        /// To do so, save those conversation ID and use them correspondingly.
        /// </summary>
        public virtual void StartNewConversation(string conversationID = null) 
            => m_ConversationID = string.IsNullOrEmpty(conversationID) ? Guid.NewGuid().ToString() : conversationID;
        
        /// <summary>
        ///     Change the method of how to select character.
        /// </summary>
        public virtual void ChangeSelectingMethod() {}
        
        /// <summary>
        /// Gets the character by a brain name.
        /// It's nullable, and will return the first one if multiple characters exist. 
        /// </summary>
        /// <param name="brainName"></param>
        /// <returns></returns>
        public virtual InworldCharacter GetCharacterByBrainName(string brainName) => CurrentCharacters.FirstOrDefault(c => c.BrainName == brainName);
        /// <summary>
        /// Gets the character by a brain name.
        /// Also supports loading by [] directly.
        /// </summary>
        /// <param name="brainName"></param>
        public virtual InworldCharacter this[string brainName] => CurrentCharacters.FirstOrDefault(c => c.BrainName == brainName);
        /// <summary>
        /// Gets the character by a given name.
        /// It's nullable, and will return the first one if multiple characters exist. 
        /// </summary>
        /// <param name="givenName"></param>
        /// <returns></returns>
        public virtual InworldCharacter GetCharacterByGivenName(string givenName) => CurrentCharacters.FirstOrDefault(c => c.Name == givenName);

        /// <summary>
        /// Add a character to the character list.
        /// Triggers OnCharacterListJoined
        /// </summary>
        /// <param name="character">target character to add.</param>
        public virtual void Register(InworldCharacter character)
        {
            if (m_CharacterList.Contains(character))
                return;
            Event.onCharacterListJoined?.Invoke(character);
            m_CharacterList.Add(character);
            InworldController.Client.UpdateConversation();
        }
        /// <summary>
        /// Remove the character from the character list.
        /// If it's current character, or in the group chat, also remove it.
        /// </summary>
        /// <param name="character">target character to remove.</param>
        public virtual bool Unregister(InworldCharacter character)
        {
            if (character == null || !InworldController.Instance)
                return false;
            if (character == CurrentCharacter) 
                CurrentCharacter = null;
            if (!m_CharacterList.Contains(character))
                return false;
            m_CharacterList.Remove(character);
            Event.onCharacterListLeft?.Invoke(character); 
            InworldController.Client.UpdateConversation();
            return true;
        }
         /// <summary>
         /// Remove all the characters from the character list.
         /// </summary>
         public bool UnregisterAll()
         {
             if (!InworldController.Instance || m_CharacterList.Count == 0)
                 return false;
             CurrentCharacter = null;
             foreach (InworldCharacter character in m_CharacterList)
                 Event.onCharacterListLeft?.Invoke(character); 
             m_CharacterList.Clear();
             return true;
         }
         
         protected virtual void OnEnable()
         {
             InworldController.Client.OnPacketReceived += ReceivePacket;
         }
         void ReceivePacket(InworldPacket packet)
         {
             if (packet is ControlPacket controlPacket && controlPacket.Action == ControlType.CONVERSATION_EVENT)
             {
                 Event.onConversationUpdated?.Invoke();
             }
         }
         protected virtual void OnDisable()
         {
             if (InworldController.Instance)
                 InworldController.Client.OnPacketReceived -= ReceivePacket;
         }
    }
}

