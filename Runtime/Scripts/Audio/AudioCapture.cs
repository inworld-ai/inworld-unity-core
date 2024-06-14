/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Inworld.Audio
{
    /// <summary>
    /// YAN: This is a global Audio Capture controller.
    ///      For each separate InworldCharacter, we use class AudioInteraction to handle audio clips.
    /// </summary>
    public class AudioCapture : MonoBehaviour
    {
        [SerializeField] protected MicSampleMode m_SamplingMode = MicSampleMode.NO_FILTER;
        [Tooltip("Hold the key to sample, release the key to send audio")]
        [SerializeField] protected KeyCode m_PushToTalkKey = KeyCode.None;
        [Range(5, 30)][SerializeField] protected float m_PlayerVolumeThreshold = 10f;
        [SerializeField] protected int m_BufferSeconds = 1;
        [SerializeField] protected int m_AudioToPushCapacity = 100;
        [SerializeField] protected string m_DeviceName;
        [SerializeField] protected bool m_DetectPlayerSpeaking = true;
        [Space(10)]
        [SerializeField] protected AudioEvent m_AudioEvent;
        
#region Variables
        protected bool m_IsAudioDebugging = false;
        protected float m_CharacterVolume = 1f;
        protected MicSampleMode m_InitSampleMode;
        protected const int k_SizeofInt16 = sizeof(short);
        protected const int k_SampleRate = 16000;
        protected const int k_Channel = 1;
        protected AudioClip m_Recording;
        protected IEnumerator m_AudioCoroutine;
        protected bool m_IsRecording;
        protected bool m_IsPlayerSpeaking;
        protected bool m_IsCapturing;
        protected float m_BackgroundNoise;
        protected float m_CalibratingTime;
        // Last known position in AudioClip buffer.
        protected int m_LastPosition;
        // Size of audioclip used to collect information, need to be big enough to keep up with collect. 
        protected int m_BufferSize;
        protected readonly ConcurrentQueue<AudioChunk> m_AudioToPush = new ConcurrentQueue<AudioChunk>();
        protected List<AudioDevice> m_Devices = new List<AudioDevice>();
        protected byte[] m_ByteBuffer;
        protected float[] m_InputBuffer;
        protected static int m_nPosition;
#endregion
        
#region Properties
        /// <summary>
        /// Gets the event handler of AudioCapture.
        /// </summary>
        public AudioEvent Event => m_AudioEvent;
        /// <summary>
        /// Gets/Sets the current playing audio source.
        /// </summary>
        public AudioSource CurrentPlayingAudioSource { get; set; }
        /// <summary>
        /// Gets the global setting of the volumes (From 0 to 1). 
        /// </summary>
        public float Volume
        {
            get => m_CharacterVolume;
            set => m_CharacterVolume = value;
        }
        /// <summary>
        /// Gets/Sets the Push to talk key.
        /// The auto detecting would only be effected if this key is NONE.
        /// </summary>
        public KeyCode PushToTalkKey
        {
            get => m_PushToTalkKey;
            set => m_PushToTalkKey = value;
        }
        /// <summary>
        /// Signifies if audio should be pushed to server automatically as it is captured.
        /// </summary>
        public bool AutoPush
        {
            get => m_SamplingMode != MicSampleMode.NO_MIC && m_SamplingMode != MicSampleMode.PUSH_TO_TALK;
            set
            {
                if (value)
                {
                    if (m_SamplingMode == MicSampleMode.PUSH_TO_TALK)
                        m_SamplingMode = m_InitSampleMode;
                }
                else
                {
                    m_SamplingMode = MicSampleMode.PUSH_TO_TALK;
                }
            }
        }
        /// <summary>
        /// The sample mode used by the Microphone. Determines how audio input is handled and processed for interactions.
        /// </summary>
        public MicSampleMode SampleMode
        {
            get => m_SamplingMode;
            set => m_SamplingMode = value;
        }
        
        /// <summary>
        /// A flag to check if player is allowed to speak and without filtering
        /// </summary>
        public bool IsPlayerTurn => 
            m_SamplingMode == MicSampleMode.NO_FILTER || 
            m_SamplingMode == MicSampleMode.PUSH_TO_TALK || 
            m_SamplingMode== MicSampleMode.TURN_BASED && !InworldController.CharacterHandler.IsAnyCharacterSpeaking;

        /// <summary>
        /// A flag to check if audio is available to send to server.
        ///     (Either Enable AEC or it's Player's turn to speak)
        /// </summary>
        public bool IsAudioAvailable => m_SamplingMode == MicSampleMode.AEC || IsPlayerTurn;
        public bool AutoDetectPlayerSpeaking
        {
            get => m_DetectPlayerSpeaking 
                   && (SampleMode != MicSampleMode.TURN_BASED || !InworldController.CharacterHandler.IsAnyCharacterSpeaking) 
                   && PushToTalkKey == KeyCode.None; 
            set => m_DetectPlayerSpeaking = value;
        }
        /// <summary>
        /// By default, it's controlled by the Record UI button in PlayerController.
        /// Note: This status is overwritten by Push to talk Hot key.
        /// </summary>
        public bool IsRecording
        {
            get => m_IsRecording || Input.GetKey(m_PushToTalkKey);
            set => m_IsRecording = value;
        }
        /// <summary>
        /// Signifies if user is speaking based on audio amplitude and threshold.
        /// </summary>
        public bool IsPlayerSpeaking
        {
            get => m_IsPlayerSpeaking;
            protected set
            {
                if (m_IsPlayerSpeaking == value)
                    return;
                m_IsPlayerSpeaking = value;
                if (m_IsPlayerSpeaking)
                    Event.onPlayerStartSpeaking?.Invoke();
                else
                    Event.onPlayerStopSpeaking?.Invoke();
            }
        }
        /// <summary>
        /// Signifies it's currently capturing.
        /// </summary>
        public bool IsCapturing
        {
            get => m_IsCapturing;
            set
            {
                if (m_IsCapturing == value)
                    return;
                m_IsCapturing = value;
                if (m_IsCapturing)
                {
                    Event.onRecordingStart?.Invoke();
                    StartAudio();
                }
                else
                {
                    Event.onRecordingEnd?.Invoke();
                    StopAudio();
                }
            }
        }
        /// <summary>
        /// Get the background noises, including music.
        /// </summary>
        public float BackgroundNoise => m_BackgroundNoise;
        /// <summary>
        /// Get Audio Input Device Name for recording.
        /// </summary>
        public string DeviceName
        {
            get
            {
                if (string.IsNullOrEmpty(m_DeviceName))
                {
                    m_DeviceName = Devices.Count == 0 ? "" : m_Devices[0].label;
                }
                return m_DeviceName;
            }
        }

        public virtual List<AudioDevice> Devices => null;
        /// <summary>
        /// Get if aec is enabled. The parent class by default is false.
        /// </summary>
        public virtual bool EnableAEC => false;
#endregion
      
#region Public Functions
        /// <summary>
        /// Change the device of microphone input.
        /// </summary>
        /// <param name="deviceName">the device name to input.</param>
        public virtual void ChangeInputDevice(string deviceName)
        {
            if (deviceName == m_DeviceName)
                return;
#if !UNITY_WEBGL
            if (Microphone.IsRecording(m_DeviceName))
                StopMicrophone(m_DeviceName);
#endif
            m_DeviceName = deviceName;
            StartMicrophone(m_DeviceName);
            Calibrate();
        }

        public void PushAudioImmediate()
        {
            if (!m_AudioToPush.TryDequeue(out AudioChunk audioChunk))
                return;
            SendAudio(audioChunk);
        }
        /// <summary>
        /// Manually push the audio wave data to server.
        /// </summary>
        public IEnumerator PushAudio()
        {
            yield return new WaitForSeconds(1);
            foreach (AudioChunk audioData in m_AudioToPush)
            {
                SendAudio(audioData);
            }
            m_AudioToPush.Clear();
        }
        public virtual void StopAudio()
        {
            m_AudioToPush.Clear();
            InworldController.Client.StopAudioTo();
        }
        public virtual void StartAudio()
        {
            MicrophoneMode mode = !EnableAEC ? MicrophoneMode.EXPECT_AUDIO_END : MicrophoneMode.OPEN_MIC;
            InworldCharacter character = InworldController.CharacterHandler.CurrentCharacter;
            if (character)
                InworldController.Client.StartAudioTo(character.BrainName, mode);
            else
                InworldController.Client.StartAudioTo(null, mode);
        }
        public virtual void SendAudio(AudioChunk chunk)
        {
            if (InworldController.Client.Status != InworldConnectionStatus.Connected)
                return;
            if (!InworldController.Client.Current.IsConversation && chunk.targetName != InworldController.Client.Current.Character?.brainName)
            {
                InworldController.Client.Current.Character = InworldController.CharacterHandler.GetCharacterByBrainName(chunk.targetName)?.Data;
            }
            InworldController.Client.SendAudioTo(chunk.chunk);
        }
        /// <summary>
        ///     Recalculate the background noise (including bg music, etc)
        ///     Please call it whenever audio environment changed in your game.
        /// </summary>
        public virtual void Calibrate()
        {
            m_BackgroundNoise = 0;
            m_CalibratingTime = 0;
        }
        protected virtual IEnumerator _Calibrate()
        {
#if !UNITY_WEBGL
            if (!Microphone.IsRecording(m_DeviceName))
                StartMicrophone(m_DeviceName);
#endif
            Event.onStartCalibrating?.Invoke();
            while (m_BackgroundNoise == 0 || m_CalibratingTime < m_BufferSeconds)
            {
                int nSize = GetAudioData();
                m_CalibratingTime += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                float rms = CalculateRMS();
                if (rms > m_BackgroundNoise)
                    m_BackgroundNoise = rms;
            }
            Event.onStopCalibrating?.Invoke();
        }
#endregion

#region MonoBehaviour Functions
        protected virtual void Awake()
        {
            Init();
        }
        
        protected virtual void OnEnable()
        {
            m_AudioCoroutine = AudioCoroutine();
            StartCoroutine(m_AudioCoroutine);
        }
        protected virtual void OnDisable()
        {
            StopCoroutine(m_AudioCoroutine);
            StopMicrophone(m_DeviceName);
        }

        protected virtual void OnDestroy()
        {
            m_Devices.Clear();
            StopMicrophone(m_DeviceName);
        }
        protected void Update()
        {
            if (m_AudioToPush.Count > m_AudioToPushCapacity)
                m_AudioToPush.TryDequeue(out AudioChunk chunk);
        }

#endregion

#region Protected Functions

        protected virtual void Init()
        {
            m_BufferSize = m_BufferSeconds * k_SampleRate;
            m_ByteBuffer = new byte[m_BufferSize * k_Channel * k_SizeofInt16];
            m_InputBuffer = new float[m_BufferSize * k_Channel];
            m_InitSampleMode = m_SamplingMode;
        }
        protected virtual IEnumerator AudioCoroutine()
        {
            while (true)
            {
                yield return _Calibrate();
                Collect();
                OutputData();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        protected virtual void Collect()
        {
            if (m_SamplingMode == MicSampleMode.NO_MIC)
                return;
            if (m_SamplingMode != MicSampleMode.PUSH_TO_TALK && m_BackgroundNoise == 0)
                return;
            int nSize = GetAudioData();
            if (nSize <= 0)
                return;
            IsPlayerSpeaking = CalculateSNR() > m_PlayerVolumeThreshold;
            IsCapturing = IsRecording || AutoDetectPlayerSpeaking && IsPlayerSpeaking;
            if (IsCapturing)
            {
                string charName = InworldController.CharacterHandler.CurrentCharacter ? InworldController.CharacterHandler.CurrentCharacter.BrainName : "";
                byte[] output = Output(nSize * m_Recording.channels);
                string audioData = Convert.ToBase64String(output);
                m_AudioToPush.Enqueue(new AudioChunk
                {
                    chunk = audioData,
                    targetName = charName
                });
            }
        }
        protected virtual void OutputData()
        {
            if (InworldController.Client && InworldController.Client.Status == InworldConnectionStatus.Connected)
                PushAudioImmediate();
            if (m_AudioToPush.Count > m_AudioToPushCapacity)
                m_AudioToPush.TryDequeue(out AudioChunk chunk);
        }
        protected virtual int GetAudioData()
        {
#if !UNITY_WEBGL
            m_nPosition = Microphone.GetPosition(m_DeviceName);
#endif
            if (m_nPosition < m_LastPosition)
                m_nPosition = m_BufferSize;
            if (m_nPosition <= m_LastPosition)
            {
                return -1;
            }
            int nSize = m_nPosition - m_LastPosition;
            if (!m_Recording || !m_Recording.GetData(m_InputBuffer, m_LastPosition))
                return -1;
            m_LastPosition = m_nPosition % m_BufferSize;
            return nSize;
        }
        public virtual void StartWebMicrophone()
        {
        }
        protected virtual byte[] Output(int nSize)
        {
            WavUtility.ConvertAudioClipDataToInt16ByteArray(m_InputBuffer, nSize * m_Recording.channels, m_ByteBuffer);
            int nWavCount = nSize * m_Recording.channels * k_SizeofInt16;
            byte[] output = new byte[nWavCount];
            Buffer.BlockCopy(m_ByteBuffer, 0, output, 0, nWavCount);
            return output;
        }
        // Root Mean Square, used to measure the variation of the noise.
        protected float CalculateRMS()
        {
            return Mathf.Sqrt(m_InputBuffer.Average(sample => sample * sample));
        }
        // Sound Noise Ratio (dB). Used to check how loud the input voice is.
        protected float CalculateSNR()
        {
            if (m_BackgroundNoise == 0)
                return 0;  // Need to calibrate first.
            return 20.0f * Mathf.Log10(CalculateRMS() / m_BackgroundNoise); 
        }
        

        public virtual void StartMicrophone(string deviceName)
        {
#if !UNITY_WEBGL
            m_Recording = Microphone.Start(deviceName, true, m_BufferSeconds, k_SampleRate);
#endif
        }
        protected virtual void StopMicrophone(string deviceName)
        {
#if !UNITY_WEBGL
            Microphone.End(deviceName);
#endif
        }
#endregion
    }
}
