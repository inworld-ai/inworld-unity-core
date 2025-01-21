/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using UnityEngine.InputSystem;

namespace Inworld.Audio
{
    public class PushToTalkModule : InworldAudioModule
    {
        InputAction m_PushToTalkInputAction;

        void Awake()
        {
            m_PushToTalkInputAction = InworldAI.InputActions["PushToTalk"];
        }

        void Update()
        {
            Audio.IsPlayerSpeaking = m_PushToTalkInputAction.IsPressed();
        }
    }
}