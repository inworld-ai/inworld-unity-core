/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

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
        [SerializeField] public KeyCode skipKey = KeyCode.None;
        [SerializeField] public KeyCode continueKey = KeyCode.None;
        [Header("Audio Capture")]
        [SerializeField] protected bool m_PushToTalk;
        [SerializeField] protected KeyCode m_PushToTalkKey = KeyCode.C;
        [Header("References")]
        [SerializeField] protected TMP_InputField m_InputField;
        
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
            InworldController.Instance.SendText(m_InputField.text);
            m_InputField.text = "";
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

            if (m_PushToTalk && m_PTTKeyPressed && !m_BlockAudioHandling)
                InworldController.Instance.StartAudio();
        }
        
        protected virtual void OnCharacterLeft(InworldCharacter newChar)
        {
            InworldAI.Log(InworldController.CharacterHandler.CurrentCharacter ? $"Now Talking to: {InworldController.CharacterHandler.CurrentCharacter.Name}" : $"Now broadcasting.");
        }
        
        protected virtual void Update()
        {
            if(m_PushToTalk && !m_BlockAudioHandling)
                HandlePTT();
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

        protected virtual void HandleInput()
        {
            if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
                SendText();
        }
    }
}

