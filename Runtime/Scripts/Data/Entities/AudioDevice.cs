/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;


namespace Inworld.Entities
{
    [Serializable]
    public class AudioSessionInfo
    {
        public string audioSessionID;
        public string currentBrainName;
        public string lastBrainName;

        /// <summary>
        /// Stops the current audio session.
        /// </summary>
        public void StopAudio()
        {
            InworldController.Client.StopAudio(); 
            lastBrainName = currentBrainName;
            currentBrainName = "";
        }
        /// <summary>
        /// Starts a new audio session.
        /// </summary>
        /// <param name="characterBrainName">The brain names of the characters to enable audio interaction</param>
        public void StartAudio(string characterBrainName)
        {
            if (string.IsNullOrEmpty(characterBrainName))
                return;
            if (currentBrainName == characterBrainName)
                return;
            StopAudio();
            InworldController.Client.StartAudioTo(characterBrainName);
            currentBrainName = characterBrainName;
        }
    }
    [Serializable]
    public class AudioDevice
    {
        public string deviceId;
        public string kind;
        public string label;
        public string groupId;
        [Preserve] public AudioDevice() {}
        [Preserve] public AudioDevice(string deviceId, string kind, string label, string groupId)
        {
            this.deviceId = deviceId;
            this.kind = kind;
            this.label = label;
            this.groupId = groupId;
        }
    }
    [Serializable]
    public class WebGLAudioDevicesData
    {
        public List<AudioDevice> devices;
    }
    
    [Serializable]
    public class WebGLAudioDeviceCapsData
    {
        public int[] caps;
    }
}
