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
using UnityEngine.InputSystem;

#if UNITY_WEBGL
using AOT;
using System.Linq;
using System.Runtime.InteropServices;
#endif


namespace Inworld
{
    /// <summary>
    /// YAN: This is a global Audio Capture controller.
    ///      For each separate InworldCharacter, we use class AudioInteraction to handle audio clips.
    /// </summary>
    public class AudioCapture : MonoBehaviour
    {
        [SerializeField] protected MicSampleMode m_SamplingMode = MicSampleMode.NO_FILTER;
        [Range(0, 30)][SerializeField] protected float m_PlayerVolumeThreshold = 10f;
        [Range(0.1f, 2f)][SerializeField] protected int m_BufferSeconds = 1;
        [SerializeField] protected int m_AudioToPushCapacity = 100;
        [SerializeField] protected string m_DeviceName;
        [SerializeField] protected bool m_DetectPlayerSpeaking = true;
        [Range(0.1f, 1f)] [SerializeField] protected float m_SwitchingAudioTimer = 0.5f;
        [SerializeField] protected bool m_MutePlayerMic;
        [Space(10)][SerializeField] protected AudioEvent m_AudioEvent;
        [Tooltip("By enabling testing mode, Inworld server will only send you the Text-To-Speech result, without any character's response.")]
        [SerializeField] protected bool m_TestMode;
#region Variables
        protected InputAction m_PushToTalkInputAction;
        protected float m_CharacterVolume = 1f;
        protected MicSampleMode m_InitSampleMode;
        protected const int k_SizeofInt16 = sizeof(short);
        protected const int k_SampleRate = 16000;
        protected const int k_Channel = 1;
        protected int m_OutputSampleRate = k_SampleRate;
        protected int m_OutputChannels = k_Channel;
        protected AudioSource m_RecordingSource;
        protected IEnumerator m_AudioCoroutine;
        protected bool m_IsRecording;
        protected bool m_IsPlayerSpeaking;
        protected bool m_IsCapturing;
        protected float m_BackgroundNoise;
        protected float m_CalibratingTime;
        protected float m_CurrentAudioSwitchingTimer;
        // Last known position in AudioClip buffer.
        protected int m_LastPosition;
        // Size of audioclip used to collect information, need to be big enough to keep up with collect. 
        protected int m_BufferSize;
        protected readonly ConcurrentQueue<AudioChunk> m_AudioToPush = new ConcurrentQueue<AudioChunk>();
        protected List<AudioDevice> m_Devices = new List<AudioDevice>();
        protected List<short> m_InputBuffer = new List<short>();
        protected float[] m_RawInput;
        protected List<short> m_ProcessedWaveData = new List<short>();
        static int m_nPosition;
#if UNITY_WEBGL
        protected static float[] s_WebGLBuffer;
        public static bool WebGLPermission { get; set; }
#endif
#endregion
        
#region Properties
        /// <summary>
        /// Gets the recording audio source
        /// </summary>
        public AudioSource Recording
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

        public bool IsMute
        {
            get => m_MutePlayerMic;
            set => m_MutePlayerMic = value;
        }

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
        /// The sample mode used by the Microphone. Determines how audio input is handled and processed for interactions.
        /// </summary>
        public MicSampleMode SampleMode
        {
            get => m_SamplingMode;
            set => m_SamplingMode = value;
        }
		/// <summary>
        /// Whether the Input Action for Push-to-Talk has bindings.
        /// </summary>
        public bool IsValidPushToTalkInput => m_PushToTalkInputAction != null && m_PushToTalkInputAction.bindings.Count > 0;
		
        /// <summary>
        /// A flag to check if player is allowed to speak and without filtering
        /// </summary>
        public bool IsPlayerTurn => IsRecording || m_SamplingMode == MicSampleMode.NO_FILTER || 
            m_SamplingMode== MicSampleMode.TURN_BASED && !InworldController.CharacterHandler.IsAnyCharacterSpeaking;

        /// <summary>
        /// A flag to check if audio is available to send to server.
        ///     (Either Enable AEC or it's Player's turn to speak)
        /// </summary>
        public bool IsAudioAvailable => m_SamplingMode == MicSampleMode.AEC || IsPlayerTurn;
        /// <summary>
        /// Gets/Sets if this component is detecting player speaking automatically.
        /// </summary>
        public bool AutoDetectPlayerSpeaking
        {
            get => m_DetectPlayerSpeaking 
                   && (SampleMode != MicSampleMode.TURN_BASED || !InworldController.CharacterHandler.IsAnyCharacterSpeaking) 
                   && !IsValidPushToTalkInput; 
            set => m_DetectPlayerSpeaking = value;
        }
        /// <summary>
        /// By default, it's controlled by the Record UI button in PlayerController.
        /// Note: This status is overwritten by Push to talk Hot key.
        /// </summary>
        public bool IsRecording
        {
            get => m_IsRecording || (IsValidPushToTalkInput && m_PushToTalkInputAction.IsPressed());
            set
            {
                m_IsRecording = value;
                if (m_IsRecording)
                    m_ProcessedWaveData.Clear();
            }
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
        /// Gets if is switching audio.
        /// </summary>
        public bool IsSwitchingAudio => m_CurrentAudioSwitchingTimer != 0;
        /// <summary>
        /// Signifies it's currently capturing.
        /// </summary>
        public bool IsCapturing
        {
            get => m_IsCapturing;
            set
            {
                if (IsSwitchingAudio)
                    return;
                if (m_IsCapturing == value)
                    return;
                m_IsCapturing = value;
                m_CurrentAudioSwitchingTimer = m_SwitchingAudioTimer;
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

        public List<AudioDevice> Devices
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (m_Devices.Count == 0)
                {
                    m_Devices = JsonUtility.FromJson<WebGLAudioDevicesData>(WebGLGetDeviceData()).devices;
                }
                return m_Devices;
#else
                return null;
#endif
            }
        }
        /// <summary>
        /// Get if aec is enabled. The parent class by default is false.
        /// </summary>
        public virtual bool EnableAEC => false;

        /// <summary>
        /// Get if VAD is enabled. The parent class by default is false.
        /// </summary>
        public virtual bool EnableVAD => false;
#endregion

#if UNITY_WEBGL && !UNITY_EDITOR 
        delegate void NativeCommand(string json);
        [DllImport("__Internal")] static extern int WebGLInit(NativeCommand handler);
        [DllImport("__Internal")] static extern int WebGLInitSamplesMemoryData(float[] array, int length);
        [DllImport("__Internal")] static extern int WebGLIsRecording();
        [DllImport("__Internal")] static extern string WebGLGetDeviceData();
        [DllImport("__Internal")] static extern string WebGLGetDeviceCaps();
        [DllImport("__Internal")] static extern int WebGLGetPosition();
        [DllImport("__Internal")] static extern void WebGLMicStart(string deviceId, int frequency, int lengthSec);
        [DllImport("__Internal")] static extern void WebGLMicEnd();
        [DllImport("__Internal")] static extern void WebGLDispose();
        [DllImport("__Internal")] static extern int WebGLIsPermitted();
#endif
        
#region Public Functions
        /// <summary>
        /// Change the device of microphone input.
        /// </summary>
        /// <param name="deviceName">the device name to input.</param>
        public void ChangeInputDevice(string deviceName)
        {
            if (deviceName == m_DeviceName)
                return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (WebGLIsRecording() == 1)
                StopMicrophone(m_DeviceName);
#else
            if (Microphone.IsRecording(m_DeviceName))
                StopMicrophone(m_DeviceName);
#endif
            m_DeviceName = deviceName;
            StartMicrophone(m_DeviceName);
            Calibrate();
        }
        /// <summary>
        /// Send the audio chunk in the queue immediately to Inworld server.
        /// </summary>
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
            MicrophoneMode micMode = IsRecording ? MicrophoneMode.EXPECT_AUDIO_END : MicrophoneMode.OPEN_MIC;
            UnderstandingMode understandingMode = m_TestMode ? UnderstandingMode.SPEECH_RECOGNITION_ONLY : UnderstandingMode.FULL;
            InworldCharacter character = InworldController.CharacterHandler.CurrentCharacter;
            if (character)
                InworldController.Client.StartAudioTo(character.BrainName, micMode, understandingMode);
            else
                InworldController.Client.StartAudioTo(null, micMode, understandingMode);
        }
        public virtual void SendAudio(AudioChunk chunk)
        {
            if (InworldController.Client.Status != InworldConnectionStatus.Connected)
                return;
            if (!InworldController.Client.Current.IsConversation && chunk.targetName != InworldController.Client.Current.Character?.brainName)
            {
                InworldController.Client.Current.Character = InworldController.CharacterHandler[chunk.targetName]?.Data;
            }
            InworldController.Client.SendAudioTo(chunk.chunk);
        }
        /// <summary>
        /// Get the audio data from the AudioListener.
        /// Need AECProbe attached to the AudioListener first.
        /// </summary>
        /// <param name="data">the output data</param>
        public virtual void GetOutputData(float[] data, int channels)
        {
            
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

#endregion

#region MonoBehaviour Functions
        protected virtual void Awake()
        {
            m_PushToTalkInputAction = InworldAI.InputActions["PushToTalk"];
            Init();
        }
        
        protected virtual void OnEnable()
        {
            m_AudioCoroutine = AudioCoroutine();
            StartCoroutine(m_AudioCoroutine);
        }
        protected virtual void OnDisable()
        {
            if (m_AudioCoroutine != null)
                StopCoroutine(m_AudioCoroutine);
            StopMicrophone(m_DeviceName);
        }
        protected virtual void OnDestroy()
        {
            m_Devices.Clear();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLDispose();
            s_WebGLBuffer = null;
#endif
            StopMicrophone(m_DeviceName);
        }
        protected void Update()
        {
            TimerCountDown();
            if (m_AudioToPush.Count > m_AudioToPushCapacity)
                m_AudioToPush.TryDequeue(out AudioChunk chunk);
        }

#endregion

#region Protected Functions
        
        protected virtual void Init()
        {
            AudioConfiguration audioSetting = AudioSettings.GetConfiguration();
            m_OutputSampleRate = audioSetting.sampleRate;
            m_OutputChannels = audioSetting.speakerMode == AudioSpeakerMode.Stereo ? 2 : 1;
            m_BufferSize = m_BufferSeconds * k_SampleRate;
            m_InitSampleMode = m_SamplingMode;
#if UNITY_WEBGL && !UNITY_EDITOR
            s_WebGLBuffer = new float[m_BufferSize * k_Channel];
            WebGLInit(OnWebGLInitialized);
#endif
        }
        protected virtual IEnumerator _Calibrate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (WebGLIsRecording() == 0)
                StartMicrophone(m_DeviceName);
            m_BackgroundNoise = 0.0001f;
            yield break;
#else
            if (!Microphone.IsRecording(m_DeviceName))
                StartMicrophone(m_DeviceName);
#endif
            Event.onStartCalibrating?.Invoke();
            if (!EnableVAD) // Use local method to calculate SNR.
            {
                Recording.volume = 0.5f;
                while (m_BackgroundNoise == 0 || m_CalibratingTime < m_BufferSeconds)
                {
                    int nSize = GetAudioData();
                    m_CalibratingTime += 0.1f;
                    yield return new WaitForSecondsRealtime(0.1f);
                    float rms = CalculateRMS();
                    if (rms > m_BackgroundNoise)
                        m_BackgroundNoise = rms;
                }
                Recording.volume = 1f;
            }
            Event.onStopCalibrating?.Invoke();
        }

        protected virtual void TimerCountDown()
        {
            m_CurrentAudioSwitchingTimer -= Time.unscaledDeltaTime;
            m_CurrentAudioSwitchingTimer = m_CurrentAudioSwitchingTimer < 0 ? 0 : m_CurrentAudioSwitchingTimer;
        }
        /// <summary>
        /// Resample all the incoming audio data to the Inworld server supported data (16000 * 1).
        /// </summary>
        protected float[] Resample(float[] inputSamples, int inputSampleRate, int inputChannels) 
        {
            int nResampleRatio = inputSampleRate * inputChannels / k_SampleRate;
            if (nResampleRatio == 1)
                return inputSamples;
            int nTargetLength = inputSamples.Length / nResampleRatio;

            float[] resamples = new float[nTargetLength];

            for (int i = 0; i < nTargetLength; i++)
            {
                int index = i * nResampleRatio;
                resamples[i] = inputSamples[index];
            }
            return resamples;
        }
        
        protected virtual IEnumerator AudioCoroutine()
        {
            while (true)
            {
                yield return _Calibrate();
                ProcessAudio();
                Collect();
                yield return OutputData();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        protected virtual void PreProcessAudioData(ref List<short> array, float[] data, int channels, bool debug = true)
        {
            float[] resampledData = debug ? data : Resample(data, m_OutputSampleRate, channels);
            foreach (float sample in resampledData)
            {
                float clampedSample = Math.Max(-1.0f, Math.Min(1.0f, sample));
                array.Add((short)(clampedSample * 32767));
            }
        }
        protected virtual void RemoveOverDueData(ref List<short> array)
        {
            if (array.Count > k_SampleRate * m_BufferSeconds)
            {
                array.RemoveRange(0, array.Count - k_SampleRate * m_BufferSeconds);
            }
        }
        
        protected virtual void ProcessAudio()
        {
            m_ProcessedWaveData.AddRange(m_InputBuffer);
            m_InputBuffer.Clear();
            RemoveOverDueData(ref m_ProcessedWaveData);
        }
        protected virtual void Collect()
        {
            if (m_SamplingMode == MicSampleMode.NO_MIC)
                return;
            if (!IsRecording && !EnableVAD && m_BackgroundNoise == 0)
                return;
            int nSize = GetAudioData();
            if (nSize <= 0)
                return;
            IsPlayerSpeaking = DetectPlayerSpeaking();
            IsCapturing = IsRecording || IsPlayerSpeaking;
            if (IsCapturing)
            {
                string charName = InworldController.CharacterHandler.CurrentCharacter ? InworldController.CharacterHandler.CurrentCharacter.BrainName : "";
                byte[] output = Output(m_ProcessedWaveData.Count);
                m_ProcessedWaveData.Clear();
                string audioData = Convert.ToBase64String(output);
                m_AudioToPush.Enqueue(new AudioChunk
                {
                    chunk = audioData,
                    targetName = charName
                });
            }
        }
        protected virtual bool DetectPlayerSpeaking() => !IsMute && AutoDetectPlayerSpeaking;

        protected virtual IEnumerator OutputData()
        {
            if (InworldController.Client && InworldController.Client.Status == InworldConnectionStatus.Connected)
                PushAudioImmediate();
            if (m_AudioToPush.Count > m_AudioToPushCapacity)
                m_AudioToPush.TryDequeue(out AudioChunk chunk);
            yield break;
        }
        // Deprecated
        protected int GetAudioData()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            m_nPosition = WebGLGetPosition();
#else
            m_nPosition = Microphone.GetPosition(m_DeviceName);
#endif
            if (m_nPosition < m_LastPosition)
                m_nPosition = m_BufferSize;
            if (m_nPosition <= m_LastPosition)
            {
                return -1;
            }
            int nSize = m_nPosition - m_LastPosition;
            
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLGetAudioData())
                return -1;
#else
            m_RawInput = new float[nSize];
            if (!Recording.clip)
                return -1;
            Recording.clip.GetData(m_RawInput, m_LastPosition);
            PreProcessAudioData(ref m_InputBuffer, m_RawInput, 1);
#endif
            m_LastPosition = m_nPosition % m_BufferSize;
            return nSize;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        public void StartWebMicrophone()
        {
            if (!WebGLPermission)
                return;
            InworldAI.Log($"Audio Input Device {DeviceName}");
            m_AudioCoroutine = AudioCoroutine();
            StartCoroutine(m_AudioCoroutine);
        }
        protected bool WebGLGetAudioData()
        {
            if (s_WebGLBuffer == null || s_WebGLBuffer.Length == 0)
                return false;
            for (int i = m_LastPosition; i < m_nPosition; i++)
            {
                float clampedSample = Math.Max(-1.0f, Math.Min(1.0f, s_WebGLBuffer[i]));
                m_InputBuffer.Add((short)(clampedSample * 32767));
            }
            return true;
        }
        [MonoPInvokeCallback(typeof(NativeCommand))]
        static void OnWebGLInitialized(string json)
        {
            try
            {
                WebGLCommand<object> command = JsonUtility.FromJson<WebGLCommandData<object>>(json).command;
                switch (command.command)
                {
                    case "PermissionChanged":
                        WebGLCommand<bool> boolCmd = JsonUtility.FromJson<WebGLCommandData<bool>>(json).command;
                        if (boolCmd.data) // Permitted.
                        {
                            WebGLPermission = true;
                            InworldController.Audio.StartWebMicrophone();
                        }
                        break;
                    case "StreamChunkReceived":
                        WebGLCommand<string> strCmd = JsonUtility.FromJson<WebGLCommandData<string>>(json).command;
                        string[] split = strCmd.data.Split(':');

                        int index = int.Parse(split[0]);
                        int length = int.Parse(split[1]);
                        int bufferLength = int.Parse(split[2]);
                        if (bufferLength == 0)
                        {
                            // Somehow the buffer will be dropped in the middle.
                            InworldAI.Log("Buffer released, reinstall");
                            WebGLInitSamplesMemoryData(s_WebGLBuffer, s_WebGLBuffer.Length); 
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                if (InworldAI.IsDebugMode)
                {
                    Debug.LogException(ex);
                }
            }
        }  
        string GetWebGLMicDeviceID(string deviceName) => m_Devices.FirstOrDefault(d => d.label == deviceName)?.deviceId;
#endif

        
        protected virtual byte[] Output(int nSize)
        {
            int nWavCount = nSize * k_SizeofInt16;
            byte[] output = new byte[nWavCount];
            Buffer.BlockCopy(m_ProcessedWaveData.ToArray(), 0, output, 0, nWavCount);
            return output;
        }
        // Root Mean Square, used to measure the variation of the noise.
        protected float CalculateRMS()
        {
            if (m_RawInput == null || m_RawInput.Length == 0)
                return 0;
            int nCount = m_RawInput.Length > 0 ? m_RawInput.Length : 1;
            double nMaxSample = 0;
            foreach (float sample in m_RawInput)
            {
                nMaxSample += (sample * sample);
            }
            return Mathf.Sqrt((float)nMaxSample / nCount);
        }
        // Sound Noise Ratio (dB). Used to check how loud the input voice is.
        protected float CalculateSNR()
        {
            if (m_BackgroundNoise == 0)
                return 0;  // Need to calibrate first.
            return 20.0f * Mathf.Log10(CalculateRMS() / m_BackgroundNoise); 
        }
        

        public void StartMicrophone(string deviceName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            deviceName = string.IsNullOrEmpty(deviceName) ? m_DeviceName : deviceName;
            string microphoneDeviceIDFromName = GetWebGLMicDeviceID(deviceName);
            if (string.IsNullOrEmpty(microphoneDeviceIDFromName))
                throw new ArgumentException("Couldn't acquire device ID for device name " + deviceName);
            if (WebGLIsRecording() == 1)
                return;
            if (Recording.clip)
                Destroy(Recording.clip);
            Recording.clip = AudioClip.Create("Microphone", k_SampleRate * m_BufferSeconds, 1, k_SampleRate, false);
            if (s_WebGLBuffer == null || s_WebGLBuffer.Length == 0)
                s_WebGLBuffer = new float[k_SampleRate];
            WebGLInitSamplesMemoryData(s_WebGLBuffer, s_WebGLBuffer.Length);
            WebGLMicStart(microphoneDeviceIDFromName, k_SampleRate, m_BufferSeconds);
#else
            Recording.clip = Microphone.Start(deviceName, true, m_BufferSeconds, k_SampleRate);
#endif
        }
        protected void StopMicrophone(string deviceName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLMicEnd();
            m_InputBuffer.Clear();
#else
            Microphone.End(deviceName);
#endif
        }
#endregion
    }
}
