/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Inworld.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Inworld.Audio
{
    public class InworldModuleException : InworldException
    {
        public InworldModuleException(string moduleName) : base($"Module {moduleName} not found")
        {
        }
    }
    [RequireComponent(typeof(AudioSource))]
    public class InworldAudioCapture : MonoBehaviour
    {
        const string k_UniqueModuleChecker = "Please ensure there is only one in the feature list.";
        [SerializeField] AudioCaptureStatus m_CurrentStatus = AudioCaptureStatus.Idle;
        [SerializeField] List<InworldAudioModule> m_AudioModules;
        [SerializeField] AudioEvent m_AudioEvent;
        
        AudioSource m_RecordingSource;
        AudioCaptureStatus m_LastStatus = AudioCaptureStatus.Idle;
        protected List<short> m_ProcessedWaveData = new List<short>();
        bool m_IsRecording = false;
        
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

        void Start()
        {
            
        }
        
        public bool StartAudio()
        {
            List<IStartAudioHandler> modules = GetModules<IStartAudioHandler>();
            if (modules.Count > 1)
                InworldAI.LogWarning("Find Multiple Modules with StartingAudio.\n");
            else if (modules.Count == 1)
                return modules[0].OnStartAudio();
            throw new InworldModuleException("StartAudio");
        }

        public bool StopAudio()
        {
            return false;
        }

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

        public List<T> GetModules<T>() => m_AudioModules.Select(module => module.GetComponent<T>()).Where(result => result != null).ToList();

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