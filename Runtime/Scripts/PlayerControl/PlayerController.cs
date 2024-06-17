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
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inworld.Sample
{
    /// <summary>
    /// This is the demo use case for how to interact with Inworld.
    /// For developers please feel free to create your own.
    /// </summary>
    public class PlayerController : SingletonBehavior<PlayerController>
    {
        [Header("References")]
        [SerializeField] protected PlayerInput m_PlayerInput;
        [SerializeField] protected TMP_InputField m_InputField;
        [SerializeField] protected TMP_Dropdown m_Dropdown;
        [SerializeField] protected Button m_SendButton;
        [SerializeField] protected Button m_RecordButton;
        public UnityEvent<string> onPlayerSpeaks;
        /// <summary>
        /// Get the PlayerInput component.
        /// </summary>
        public PlayerInput PlayerInput => m_PlayerInput;
        /// <summary>
        /// Get if any canvas is open.
        /// </summary>
        public virtual bool IsAnyCanvasOpen => true;
        /// <summary>
        /// Get the named Input Action from the PlayerInput component.
        /// </summary>
        /// <param name="actionName">The name of the Input Action to return.</param>
        /// <returns>The Input Action if it exists, otherwise returns null.</returns>
        public InputAction GetInputAction(string actionName)
        {
            return m_PlayerInput.actions.FindAction(actionName);
        }
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

        protected virtual void OnEnable()
        {
            InworldController.CharacterHandler.Event.onCharacterListJoined.AddListener(OnCharacterJoined);
            InworldController.CharacterHandler.Event.onCharacterListLeft.AddListener(OnCharacterLeft);
        }
        protected virtual void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.CharacterHandler.Event.onCharacterListJoined.RemoveListener(OnCharacterJoined);
            InworldController.CharacterHandler.Event.onCharacterListLeft.RemoveListener(OnCharacterLeft);
        }


        protected virtual void OnCharacterJoined(InworldCharacter newChar)
        {
            InworldAI.Log($"{newChar.Name} joined.");
            if (m_Dropdown)
            {
                m_Dropdown.options.Add(new TMP_Dropdown.OptionData
                {
                    text = newChar.Name
                });
                if (m_Dropdown.options.Count > 0)
                    m_Dropdown.gameObject.SetActive(true);
            }
        }
        
        protected virtual void OnCharacterLeft(InworldCharacter newChar)
        {
            InworldAI.Log($"{newChar.Name} left.");
            if (m_Dropdown)
            {
                TMP_Dropdown.OptionData option = m_Dropdown.options.FirstOrDefault(o => o.text == newChar.Name);
                if (option != null)
                    m_Dropdown.options.Remove(option);
                if (m_Dropdown.options.Count <= 0)
                    m_Dropdown.gameObject.SetActive(false);
            }
        }
        
        protected virtual void Update()
        {
            HandleCanvas();
            HandleInput();
        }
        public virtual void OpenFeedback(string interactionID, string correlationID)
        {
            
        }
        protected virtual void HandleCanvas()
        {
            
        }
        protected virtual void HandleInput()
        {
            if (m_PlayerInput.actions["Submit"].WasReleasedThisFrame())
                SendText();
        }
    }
}

