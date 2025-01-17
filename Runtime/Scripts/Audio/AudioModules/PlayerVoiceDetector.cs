/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Inworld.Audio
{
    public class PlayerVoiceDetector : InworldAudioModule, ICalibrateAudioHandler
    {
        [SerializeField] [Range(0.1f, 1f)] float m_BufferSeconds = 0.5f; 
        [SerializeField] [Range(0.1f, 5f)] float m_MinAudioSessionDuration = 0.5f;
        [SerializeField] [Range(10f, 100f)] float m_PlayerVolumeThreashold = 30f;
        
        protected float m_BackgroundNoise;
        protected float m_CalibratingTime;
        protected float m_AudioSessionSwitchingTime;
        
        void OnEnable()
        {
            StartModule(VoiceDetectionCoroutine());
        }

        void OnDisable()
        {
            StopModule();
        }

        IEnumerator VoiceDetectionCoroutine()
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
            double nMaxSample = Audio.ProcessedWaveData.Where(f => f > 0).Aggregate<short, double>(0, (current, sample) => current + (float)sample / short.MaxValue * sample / short.MaxValue);
            return Mathf.Sqrt((float)nMaxSample / Audio.ProcessedWaveData.Count);
        }
        protected float CalculateSNR()
        {
            if (m_BackgroundNoise == 0)
                return 0;  // Need to calibrate first.
            return 20.0f * Mathf.Log10(CalculateRMS() / m_BackgroundNoise); 
        }
    }
}