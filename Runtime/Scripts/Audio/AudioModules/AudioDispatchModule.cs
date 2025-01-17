/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Inworld.Entities;
using UnityEngine;

namespace Inworld.Audio
{
    public class AudioDispatchModule: InworldAudioModule, ISendAudioHandler
    {
        [SerializeField] MicrophoneMode m_SamplingMode = MicrophoneMode.OPEN_MIC;
        [SerializeField] bool m_TestMode;
        ConcurrentQueue<AudioChunk> m_AudioToSend = new ConcurrentQueue<AudioChunk>();
        int m_LastPosition;
        int m_CurrPosition;

        public MicrophoneMode SendingMode
        {
            get => m_SamplingMode;
            set => m_SamplingMode = value;
        }
        void OnEnable()
        {
            Audio.Event.onPlayerStartSpeaking.AddListener(OnStartSendAudio);
            Audio.Event.onPlayerStopSpeaking.AddListener(OnStopSendAudio);
            StartModule(AudioDispatchingCoroutine());
        }
        void OnDisable()
        {
            Audio.Event.onPlayerStartSpeaking.RemoveListener(OnStartSendAudio);
            Audio.Event.onPlayerStopSpeaking.RemoveListener(OnStopSendAudio);
            StopModule();
        }

        AudioChunk GetAudioChunk(List<short> data)
        {
            AudioChunk chunk = new AudioChunk();
            return chunk;
        }
        IEnumerator AudioDispatchingCoroutine()
        {
            while (isActiveAndEnabled)
            {
                if (Audio.IsPlayerSpeaking)
                {
                    if (m_AudioToSend != null 
                        && m_AudioToSend.Count > 0 
                        && m_AudioToSend.TryDequeue(out AudioChunk audioChunk))
                    {
                        if (!OnSendAudio(audioChunk))
                            InworldAI.LogWarning($"Sending Audio to {audioChunk.targetName} Failed. ");
                    }
                    if (m_LastPosition != ShortBufferToSend.lastPos || m_CurrPosition != ShortBufferToSend.currPos)
                    {
                        m_AudioToSend?.Enqueue(GetAudioChunk(ShortBufferToSend.Dequeue()));
                    }
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        public CircularBuffer<short> ShortBufferToSend
        {
            get
            {
                IProcessAudioHandler processor = Audio.GetModule<IProcessAudioHandler>();
                return processor == null ? Audio.InputBuffer : processor.ProcessedBuffer;
            }
        }

        public void OnStartSendAudio()
        {
            UnderstandingMode understandingMode = m_TestMode ? UnderstandingMode.SPEECH_RECOGNITION_ONLY : UnderstandingMode.FULL;
            InworldCharacter character = InworldController.CharacterHandler.CurrentCharacter;
            InworldController.Client.StartAudioTo(character ? character.BrainName : null, m_SamplingMode,
                understandingMode);
        }

        public void OnStopSendAudio()
        {
            while (!m_AudioToSend.IsEmpty)
            {
                if (m_AudioToSend.TryDequeue(out AudioChunk audioChunk))
                {
                    OnSendAudio(audioChunk);
                }
            }
            InworldController.Client.StopAudioTo();
        }

        public bool OnSendAudio(AudioChunk chunk)
        {
            if (!InworldController.Client.Current.IsConversation && chunk.targetName != InworldController.Client.Current.Character?.brainName)
            {
                InworldController.Client.Current.Character = InworldController.CharacterHandler[chunk.targetName]?.Data;
            }
            return InworldController.Client.SendAudioTo(chunk.chunk);
        }
    }
}