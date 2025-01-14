/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using UnityEngine;

namespace Inworld.Audio
{
    public abstract class AudioCaptureModule : InworldAudioModule, IMicrophoneHandler
    {
        protected const int k_InputSampleRate = 16000;
        protected const int k_InputChannels = 1;
        protected const int k_InputBufferSecond = 1;

        public virtual bool StartMicrophone()
        {
            Audio.RecordingClip = Microphone.Start(Audio.DeviceName, true, k_InputBufferSecond, k_InputSampleRate);
            Audio.ResetPointer();
            return Audio.RecordingClip;
        }
        public virtual bool ChangeInputDevice(string deviceName)
        {
            if (deviceName == Audio.DeviceName)
                return true;

            if (IsMicRecording)
                StopMicrophone();

            Audio.DeviceName = deviceName;
            return StartMicrophone();
        }
        public virtual bool StopMicrophone()
        {
            Microphone.End(Audio.DeviceName);
            Audio.InputBuffer.Clear();
            return true;
        }
        public virtual bool IsMicRecording => Microphone.IsRecording(Audio.DeviceName);
    }
}