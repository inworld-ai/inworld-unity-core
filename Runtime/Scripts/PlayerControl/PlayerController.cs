/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Inworld.Sample
{
    /// <summary>
    /// This is the demo use case for how to interact with Inworld.
    /// For developers please feel free to create your own.
    /// </summary>
    public class PlayerController : SingletonBehavior<PlayerController>
    {
        [Header("Interaction Control")]
        [SerializeField] public KeyCode uiKey = KeyCode.BackQuote;
        [SerializeField] public KeyCode optionKey = KeyCode.Escape;
        [SerializeField] public KeyCode skipKey = KeyCode.None;
        [SerializeField] public KeyCode continueKey = KeyCode.None;
        [Header("Audio Capture")]
        [SerializeField] protected bool m_PushToTalk;
        [SerializeField] protected KeyCode m_PushToTalkKey = KeyCode.C;
        [Header("References")]
        [SerializeField] protected TMP_InputField m_InputField;
        [SerializeField] protected TMP_Dropdown m_Dropdown;
        [SerializeField] protected Button m_SendButton;
        [SerializeField] protected Button m_RecordButton;
        public UnityEvent<string> onPlayerSpeaks;

        protected bool m_PTTKeyPressed;
        protected bool m_BlockAudioHandling;

        /// <summary>
        /// Send target message in the input field.
        /// </summary>
        public void SendText()
        {
            if (!m_InputField || string.IsNullOrEmpty(m_InputField.text))
                return;
            string text = m_InputField.text;
            if (text.StartsWith("*"))
                InworldController.Instance.SendNarrativeAction(text.Remove(0, 1));
            else
                InworldController.Instance.SendText(text);
            m_InputField.text = "";
        }
        /// <summary>
        /// Select the character by the default dropdown component.
        /// </summary>
        /// <param name="nIndex">the index in the drop down</param>
        public virtual void SelectCharacterByDropDown(Int32 nIndex)
        {
            if (!m_Dropdown)
                return;
            if (nIndex < 0 || nIndex > m_Dropdown.options.Count)
                return;
            if (nIndex == 0) // NONE
            {
                InworldController.CharacterHandler.CurrentCharacter = null;
                return;
            }
            InworldCharacter character = InworldController.CharacterHandler.GetCharacterByGivenName(m_Dropdown.options[nIndex].text);
            if (!character || character == InworldController.CharacterHandler.CurrentCharacter)
                return;
            InworldController.CharacterHandler.CurrentCharacter = character;
        }
        protected virtual void Start()
        {
            InworldController.CharacterHandler.ManualAudioHandling = m_PushToTalk;
            InworldController.Audio.AutoPush = !m_PushToTalk;
        }
        protected virtual void OnEnable()
        {
            InworldController.Client.OnStatusChanged += OnStatusChanged;
            InworldController.CharacterHandler.OnCharacterListJoined += OnCharacterJoined;
            InworldController.CharacterHandler.OnCharacterListLeft += OnCharacterLeft;
        }
        protected virtual void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.Client.OnStatusChanged -= OnStatusChanged;
            InworldController.CharacterHandler.OnCharacterListJoined -= OnCharacterJoined;
            InworldController.CharacterHandler.OnCharacterListLeft -= OnCharacterLeft;
        }
        
        protected virtual void OnStatusChanged(InworldConnectionStatus newStatus)
        {
            if (newStatus == InworldConnectionStatus.Connected)
            {
                if (m_PushToTalk && m_PTTKeyPressed && !m_BlockAudioHandling)
                    InworldController.Instance.StartAudio();
            }
            else
            {
                if (m_PushToTalk && !m_PTTKeyPressed && !m_BlockAudioHandling)
                    InworldController.Instance.StopAudio();
            }
        }

        protected virtual void OnCharacterJoined(InworldCharacter newChar)
        {
            InworldAI.Log($"Now Talking to: {newChar.Name}");
            if (m_Dropdown)
            {
                m_Dropdown.options.Add(new TMP_Dropdown.OptionData
                {
                    text = newChar.Name
                });
                if (m_Dropdown.options.Count > 0)
                    m_Dropdown.gameObject.SetActive(true);
            }
            if (m_PushToTalk && m_PTTKeyPressed && !m_BlockAudioHandling)
                InworldController.Instance.StartAudio();
        }
        
        protected virtual void OnCharacterLeft(InworldCharacter newChar)
        {
            if (m_Dropdown)
            {
                TMP_Dropdown.OptionData option = m_Dropdown.options.FirstOrDefault(o => o.text == newChar.Name);
                if (option != null)
                    m_Dropdown.options.Remove(option);
                if (m_Dropdown.options.Count <= 0)
                    m_Dropdown.gameObject.SetActive(false);
            }
            InworldAI.Log(InworldController.CharacterHandler.CurrentCharacter ? $"Now Talking to: {InworldController.CharacterHandler.CurrentCharacter.Name}" : $"Now broadcasting.");
        }
        
        protected virtual void Update()
        {
            if(m_PushToTalk && !m_BlockAudioHandling)
                HandlePTT();
            HandleCanvas();
            HandleInput();
        }
        
        protected virtual void HandlePTT()
        {
            if (Input.GetKeyDown(m_PushToTalkKey))
            {
                m_PTTKeyPressed = true;
                InworldController.Instance.StartAudio();
            }
            else if (Input.GetKeyUp(m_PushToTalkKey))
            {
                m_PTTKeyPressed = false;
                InworldController.Instance.PushAudio();
            }
        }
        public virtual void OpenFeedback(string interactionID, string correlationID)
        {
            
        }
        protected virtual void HandleCanvas()
        {
            
        }
        protected virtual void HandleInput()
        {
            if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
                SendText();
        }
    }
}

