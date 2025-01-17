/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Concurrent;
using Inworld.Entities;
using UnityEngine;

namespace Inworld.Audio
{
    /// <summary>
    /// The basic module class. All the module related interfaces are also put in the same file.
    /// </summary>
    public abstract class InworldAudioModule : MonoBehaviour
    {
        public int Priority {get; set;}

        InworldAudioCapture m_Capture;
        IEnumerator m_ModuleCoroutine;

        public InworldAudioCapture Audio
        {
            get
            {
                if (m_Capture != null)
                    return m_Capture;
                m_Capture = InworldController.Audio;
                return m_Capture;
            }
        }
        public virtual void StartModule(IEnumerator moduleCycle)
        {
            if (moduleCycle == null || m_ModuleCoroutine != null) 
                return;
            m_ModuleCoroutine = moduleCycle;
            StartCoroutine(m_ModuleCoroutine);
        }

        public virtual void StopModule()
        {
            if (m_ModuleCoroutine == null) 
                return;
            StopCoroutine(m_ModuleCoroutine);
            m_ModuleCoroutine = null;
        }
    }
    public class ModuleNotFoundException : InworldException
    {
        public ModuleNotFoundException(string moduleName) : base($"Module {moduleName} not found")
        {
        }
    }

    public interface IMicrophoneHandler
    {
        bool IsMicRecording {get;}
        bool StartMicrophone();
        bool ChangeInputDevice(string deviceName);
        bool StopMicrophone();
    }

    public interface ICollectAudioHandler
    {
        int OnCollectAudio();
        void ResetPointer();
    }

    public interface ICalibrateAudioHandler
    {
        void OnStartCalibration();
        void OnStopCalibration();
        void OnCalibrate();
    }

    public interface IProcessAudioHandler
    {
        bool OnPreProcessAudio();
        bool OnPostProcessAudio();
        CircularBuffer<short> ProcessedBuffer { get; set; }
    }

    public interface ISendAudioHandler
    {
        MicrophoneMode SendingMode { get; set; }
        void OnStartSendAudio();
        void OnStopSendAudio();
        bool OnSendAudio(AudioChunk audioChunk);
    }
}