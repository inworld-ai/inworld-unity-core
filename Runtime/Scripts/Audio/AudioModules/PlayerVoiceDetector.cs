/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inworld.Audio
{
    public class PlayerVoiceDetector : PlayerEventModule, ICalibrateAudioHandler
    {
        [SerializeField] [Range(0.1f, 1f)] float m_BufferSeconds = 0.5f; 
        [SerializeField] [Range(0.1f, 5f)] float m_MinAudioSessionDuration = 0.5f;
        [SerializeField] [Range(10f, 100f)] float m_PlayerVolumeThreashold = 30f;
        
        protected float m_BackgroundNoise;
        protected float m_CalibratingTime;
        protected float m_AudioSessionSwitchingTime;

        public CircularBuffer<short> ShortBufferToSend
        {
            get
            {
                IProcessAudioHandler processor = Audio.GetModule<IProcessAudioHandler>();
                return processor == null ? Audio.InputBuffer : processor.ProcessedBuffer;
            }
        }
        
        public override IEnumerator OnPlayerUpdate()
        {
            while (isActiveAndEnabled)
            {
                if (Audio.IsCalibrating)
                    OnCalibrate();
                else
                {
                    bool isPlayerSpeaking = CalculateSNR() > m_PlayerVolumeThreashold;
                    if (isPlayerSpeaking)
                    {
                        m_AudioSessionSwitchingTime = 0;
                        Audio.IsPlayerSpeaking = true;
                    }
                    else
                    {
                        m_AudioSessionSwitchingTime += 0.1f;
                        if (m_AudioSessionSwitchingTime >= m_MinAudioSessionDuration)
                            Audio.IsPlayerSpeaking = false;
                    }
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        public void OnStartCalibration()
        {
            Audio.IsCalibrating = true;
        }

        public void OnStopCalibration()
        {
            Audio.IsCalibrating = false;
        }
        public void OnCalibrate()
        {
            float rms = CalculateRMS();
            if (rms > m_BackgroundNoise)
                m_BackgroundNoise = rms;
            m_CalibratingTime += Time.fixedUnscaledDeltaTime;
            if (m_CalibratingTime >= m_BufferSeconds)
                OnStopCalibration();
        }
        
        // Root Mean Square, used to measure the variation of the noise.
        protected float CalculateRMS()
        {
            List<short> data = ShortBufferToSend.Dequeue();
            double nMaxSample = data.Aggregate<short, double>(0, (current, f) => current + (float)f / short.MaxValue * f / short.MaxValue);
            return Mathf.Sqrt((float)nMaxSample / data.Count);
        }
        protected float CalculateSNR()
        {
            float backgroundNoise = m_BackgroundNoise == 0 ? 0.001f : m_BackgroundNoise; 
            return 20.0f * Mathf.Log10(CalculateRMS() / backgroundNoise); 
        }
    }
}