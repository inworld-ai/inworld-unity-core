/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using UnityEngine;

namespace Inworld.Audio
{
    public class AudioCollectModule : InworldAudioModule, ICollectAudioHandler
    {
        protected int m_LastPosition;
        protected int m_CurrPosision;
        
        public virtual int OnCollectAudio()
        {
            string deviceName = Audio.DeviceName;
            if (!Audio.IsMicRecording())
            {
                Audio.StartMicrophone();
            }
            AudioClip recClip = Audio.RecordingClip;
            if (!recClip)
                return -1;
            m_CurrPosision = Microphone.GetPosition(deviceName);
            if (m_CurrPosision < m_LastPosition)
                m_CurrPosision = recClip.samples;
            if (m_CurrPosision <= m_LastPosition)
            {
                return -1;
            }
            int nSize = m_CurrPosision - m_LastPosition;
            Audio.RawInput = new float[nSize];
            recClip.GetData(Audio.RawInput, m_LastPosition);
            Audio.InputBuffer = WavUtility.ConvertAudioClipDataToInt16Queue(Audio.RawInput, recClip.frequency, recClip.channels);
            m_LastPosition = m_CurrPosision % recClip.samples;
            return nSize;
        }

        public void ResetPointer() => m_LastPosition = m_CurrPosision = 0;
    }
}