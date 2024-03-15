/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;


namespace Inworld.Entities
{
    [Serializable]
    public class AudioSessionInfo
    {
        public string audioSessionID;
        public List<string> currentBrainNames = new List<string>();
        public List<string> lastBrainNames = new List<string>();
        public bool IsLive => currentBrainNames.Count > 0;

        public void StopAudio()
        {
            InworldController.Client.StopAudioTo(currentBrainNames);
            lastBrainNames = currentBrainNames;
            currentBrainNames.Clear();
        }
        public void StartAudio(List<string> characterBrainNames)
        {
            if (characterBrainNames.Count == 0)
                return;
            if (CharactersAreSame(characterBrainNames))
                return;
            StopAudio();
            InworldController.Client.StartAudioTo(characterBrainNames);
            currentBrainNames = characterBrainNames;
        }
        public bool CharactersAreSame(List<string> characterBrainNames)
        {
            return currentBrainNames.Count == characterBrainNames.Count && currentBrainNames.All(characterBrainNames.Contains);
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
