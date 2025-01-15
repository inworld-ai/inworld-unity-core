/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Inworld.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Inworld.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class InworldAudioCapture : MonoBehaviour
    {
        const string k_UniqueModuleChecker = "Find Multiple Modules with StartingAudio.\nPlease ensure there is only one in the feature list.";
        [SerializeField] AudioCaptureStatus m_CurrentStatus = AudioCaptureStatus.Idle;
        [SerializeField] List<InworldAudioModule> m_AudioModules;
        [SerializeField] AudioEvent m_AudioEvent;
        [SerializeField] string m_DeviceName;
        
        AudioSource m_RecordingSource;
        AudioCaptureStatus m_LastStatus = AudioCaptureStatus.Idle;
        protected float[] m_RawInput;
        protected ConcurrentQueue<short> m_InputBuffer = new ConcurrentQueue<short>();
        protected List<short> m_ProcessedWaveData = new List<short>();
        IEnumerator m_AudioCoroutine;

        bool m_IsRecording = false;

        public float[] RawInput
        {
            get => m_RawInput;
            set => m_RawInput = value;
        }

        public ConcurrentQueue<short> InputBuffer
        {
            get => m_InputBuffer;
            set => m_InputBuffer = value;
        }

        public List<short> ProcessedWaveData => m_ProcessedWaveData;
        public string DeviceName
        {
            get => m_DeviceName;
            set => m_DeviceName = value;
        }
        public AudioSource RecordingSource
        {
            get             
            {
                if (m_RecordingSource)
                    return m_RecordingSource;
                m_RecordingSource = GetComponent<AudioSource>();
                if (!m_RecordingSource)
                    m_RecordingSource = gameObject.AddComponent<AudioSource>();
                return m_RecordingSource;
            }
        }
        public AudioClip RecordingClip
        {
            get => RecordingSource?.clip;
            set
            {
                if (RecordingSource)
                    RecordingSource.clip = value;
            }
        }

        public AudioEvent Event => m_AudioEvent;
        public AudioCaptureStatus Status
        {
            get => m_CurrentStatus;
            set
            {
                if (m_CurrentStatus == value)
                    return;
                m_LastStatus = m_CurrentStatus;
                m_CurrentStatus = value;
                Event.OnAudioStatusExit?.Invoke(m_LastStatus);
                Event.OnAudioStatusEnter?.Invoke(m_CurrentStatus);
            }
        }

        public bool IsRecording
        {
            get => m_IsRecording;
            set => _SetBoolWithEvent(ref m_IsRecording, value, Event.onRecordingStart, Event.onRecordingEnd);
        }

        public bool IsAudioAvailable => m_ProcessedWaveData?.Count > 0;
        public float Volume { get; set; }


        protected virtual IEnumerator AudioCoroutine()
        {
            while (true)
            {
                // int nSize = GetAudioData();
                // if (nSize > 0)
                // {
                //     ProcessAudio();
                //     Calibrate();
                //     Collect();
                //     yield return OutputData();
                // }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        public bool IsMicRecording() => GetUniqueModule<IMicrophoneHandler>()?.IsMicRecording ?? false;
        
        
        public bool StartMicrophone()
        {
            IMicrophoneHandler micHandler = GetUniqueModule<IMicrophoneHandler>();
            if (micHandler == null)
                return false;
            micHandler.StartMicrophone();
            return true;
        }

        //TODO(Yan): Make it void.
        public bool StopMicrophone()
        {
            IMicrophoneHandler micHandler = GetUniqueModule<IMicrophoneHandler>();
            if (micHandler == null)
                return false;
            micHandler.StopMicrophone();
            return true;
        }

        public void StartAudioThread()
        {
            if (m_AudioCoroutine != null) 
                return;
            m_AudioCoroutine = AudioCoroutine();
            StartCoroutine(m_AudioCoroutine);
        }

        public void StopAudioThread()
        {
            if (m_AudioCoroutine == null) 
                return;
            StopCoroutine(m_AudioCoroutine);
            m_AudioCoroutine = null;
        }
        public void ResetPointer() => GetUniqueModule<ICollectAudioHandler>()?.ResetPointer();
        public void StartCalibrate() => GetModules<ICalibrateAudioHandler>().ForEach(module => module.OnStartCalibration());
        public void StopCalibrate() => GetModules<ICalibrateAudioHandler>().ForEach(module => module.OnStopCalibration());
        public bool SendAudio(AudioChunk audioChunk)
        {
            return false;
        }

        /// <summary>
        /// Manually push the audio wave data to server.
        /// </summary>
        public IEnumerator PushAudio()
        {
            yield return new WaitForSeconds(1f);
        }

        public T GetModule<T>() => m_AudioModules.Select(module => module.GetComponent<T>()).FirstOrDefault(result => result != null);

        public T GetUniqueModule<T>()
        {
            List<T> modules = GetModules<T>();
            if (modules == null || modules.Count == 0) 
                throw new ModuleNotFoundException(typeof(T).Name);
            if (modules.Count > 1)
                InworldAI.LogWarning(k_UniqueModuleChecker);
            return modules[0];
        }

        public List<T> GetModules<T>() => m_AudioModules.Select(module => module.GetComponent<T>()).Where(result => result != null).ToList();

        void OnDestroy()
        {
            StopMicrophone();
        }

        void _SetBoolWithEvent(ref bool flag, bool value, UnityEvent onEvent, UnityEvent offEvent)
        {
            if (flag == value)
                return;
            flag = value;
            if (flag)
                onEvent?.Invoke();
            else
                offEvent?.Invoke();
        }

    }
}