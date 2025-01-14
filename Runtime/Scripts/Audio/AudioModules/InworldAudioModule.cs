﻿/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using UnityEngine;

namespace Inworld.Audio
{
    public abstract class InworldAudioModule : MonoBehaviour
    {
        InworldAudioCapture m_Capture;

        public InworldAudioCapture Handler
        {
            get
            {
                if (m_Capture != null)
                    return m_Capture;
                m_Capture = InworldController.Audio;
                return m_Capture;
            }
        }
    }

    public interface IStartAudioHandler
    {
        bool OnStartAudio();
    }
}