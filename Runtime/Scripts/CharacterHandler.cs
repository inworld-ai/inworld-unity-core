/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Inworld.Entities;

namespace Inworld
{
    public enum CharSelectingMethod
    {
        Manual,
        KeyCode,
        SightAngle,
        AutoChat
    }
    public class CharacterHandler : MonoBehaviour
    {
        [SerializeField] bool m_ManualAudioHandling;
        InworldCharacter m_CurrentCharacter;

        string m_ConversationID;
        public event Action<InworldCharacter> OnCharacterListJoined;
        public event Action<InworldCharacter> OnCharacterListLeft;
        
        /// <summary>
        /// Gets/Sets the ConversationID. 
        /// </summary>
        public string ConversationID
        {
            get => m_ConversationID;
            set
            {
                m_ConversationID = value;
                InworldController.Client.UpdateConversation(CurrentCharacterNames);
            }
        }

        // The Character List only lists the interactable characters. 
        // Although InworldCharacter also has InworldCharacterData, its agentID won't be always updated. Please check m_LiveSession
        // and Call RegisterLiveSession if outdated.
        protected readonly List<InworldCharacter> m_CharacterList = new List<InworldCharacter>();
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
        ///     Create a new conversationID.
        ///     You can create multiple conversationIDs and categorize with specific characters to have a clear chat history.
        /// </summary>
        public virtual void CreateConversation() => m_ConversationID = Guid.NewGuid().ToString();

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
        /// Gets the character by a given name.
        /// It's nullable, and will return the first one if multiple characters exist. 
        /// </summary>
        /// <param name="givenName"></param>
        /// <returns></returns>
        public virtual InworldCharacter GetCharacterByGivenName(string givenName) => CurrentCharacters.FirstOrDefault(c => c.Name == givenName);
        IEnumerator UpdateThumbnail(InworldCharacterData agent)
        {
            if (agent.thumbnail)
                yield break;
            string url = agent.characterAssets?.ThumbnailURL;
            if (string.IsNullOrEmpty(url))
                yield break;
            UnityWebRequest uwr = new UnityWebRequest(url);
            uwr.downloadHandler = new DownloadHandlerTexture();
            yield return uwr.SendWebRequest();
            if (uwr.isDone && uwr.result == UnityWebRequest.Result.Success)
            {
                agent.thumbnail = (uwr.downloadHandler as DownloadHandlerTexture)?.texture;
            }
        }
        /// <summary>
        /// Add a character to the character list.
        /// Triggers OnCharacterListJoined
        /// </summary>
        /// <param name="character">target character to add.</param>
        public virtual void Register(InworldCharacter character)
        {
            if (m_CharacterList.Contains(character))
                return;
            m_CharacterList.Add(character);
            OnCharacterListJoined?.Invoke(character);
            InworldController.Client.UpdateConversation(CurrentCharacterNames);
        }
        /// <summary>
        /// Remove the character from the character list.
        /// If it's current character, or in the group chat, also remove it.
        /// </summary>
        /// <param name="character">target character to remove.</param>
        public virtual void Unregister(InworldCharacter character)
        {
            if (character == null || !InworldController.Instance)
                return;
            if (character == CurrentCharacter) 
                CurrentCharacter = null;
            if (!m_CharacterList.Contains(character))
                return;
            m_CharacterList.Remove(character);
            InworldController.Client.UpdateConversation(CurrentCharacterNames);
            OnCharacterListLeft?.Invoke(character); 
        }
         /// <summary>
         /// Remove all the characters from the character list.
         /// </summary>
         public void UnregisterAll()
         {
             if (!InworldController.Instance || m_CharacterList.Count == 0)
                 return;
             CurrentCharacter = null;
             foreach (InworldCharacter character in m_CharacterList)
                 OnCharacterListLeft?.Invoke(character); 
             m_CharacterList.Clear();
         }
         void Awake()
         {
             CreateConversation();
         }
    }
}

