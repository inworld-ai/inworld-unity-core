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
        protected int m_CurrentUILayers;
        public UnityEvent<string> onPlayerSpeaks;
        public UnityEvent onCanvasOpen; 
        public UnityEvent onCanvasClosed;

        public int UILayer
        {
            get => m_CurrentUILayers;
            set
            {
                m_CurrentUILayers = value;
                if (m_CurrentUILayers > 0)
                    onCanvasOpen?.Invoke();
                else
                    onCanvasClosed?.Invoke();
            }
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
        }
        
        protected virtual void OnCharacterLeft(InworldCharacter newChar)
        {
            InworldAI.Log($"{newChar.Name} left.");
        }
        
        protected virtual void Update()
        {
            HandleInput();
        }
        public virtual void OpenFeedback(string interactionID, string correlationID)
        {
            
        }

        protected virtual void HandleInput()
        {

        }
    }
}

