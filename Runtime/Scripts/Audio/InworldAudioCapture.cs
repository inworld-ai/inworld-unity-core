/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Inworld.Audio
{
    public class InworldAudioCapture : MonoBehaviour
    {
        [SerializeField] List<InworldAudioModule> m_AudioModules;
    }
}