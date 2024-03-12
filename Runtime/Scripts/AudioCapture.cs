/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using Inworld.Packet;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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
        [Range(1, 2)][SerializeField] protected float m_PlayerVolumeThreshold = 2f;
        [SerializeField] protected int m_BufferSeconds = 1;
        [SerializeField] protected string m_DeviceName;

        public AudioSource CurrentPlayingPlayer { get; set; }
        public UnityEvent OnRecordingStart;
        public UnityEvent OnRecordingEnd;
        public UnityEvent OnPlayerStartSpeaking;
        public UnityEvent OnPlayerStopSpeaking;
        
#region Variables
        protected MicSampleMode m_LastSampleMode;
        protected const int k_SizeofInt16 = sizeof(short);
        protected const int k_SampleRate = 16000;
        protected const int k_Channel = 1;
        protected AudioClip m_Recording;
        protected IEnumerator m_AudioCoroutine;
        protected bool m_IsPlayerSpeaking;
        protected bool m_IsCapturing;
        protected float m_BackgroundNoise;
        // Last known position in AudioClip buffer.
        protected int m_LastPosition;
        // Size of audioclip used to collect information, need to be big enough to keep up with collect. 
        protected int m_BufferSize;
        protected readonly List<string> m_AudioToPush = new List<string>();
        protected List<AudioDevice> m_Devices = new List<AudioDevice>();
        protected byte[] m_ByteBuffer;
        protected float[] m_InputBuffer;
        protected AudioSessionInfo m_CurrentAudioSession;
        static int m_nPosition;
#if UNITY_WEBGL
        protected static float[] s_WebGLBuffer;
        public static bool WebGLPermission { get; set; }
#endif
#endregion
        
#region Properties
        /// <summary>
        /// Signifies if audio is currently blocked from being captured.
        /// </summary>
        public bool IsBlocked { get; set; }
        /// <summary>
        /// Signifies if microphone is capturing audio.
        /// </summary>
        public bool IsCapturing => m_IsCapturing;
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
                        m_SamplingMode = m_LastSampleMode;
                }
                else
                {
                    if (m_SamplingMode != MicSampleMode.PUSH_TO_TALK)
                        m_LastSampleMode = m_SamplingMode;
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
            m_SamplingMode == MicSampleMode.TURN_BASED && !InworldController.CharacterHandler.IsAnyCharacterSpeaking;

        /// <summary>
        /// A flag to check if audio is available to send to server.
        ///     (Either Enable AEC or it's Player's turn to speak)
        /// </summary>
        public bool IsAudioAvailable => m_SamplingMode == MicSampleMode.AEC || IsPlayerTurn;

        /// <summary>
        /// Signifies if user is speaking based on audio amplitude and threshold.
        /// </summary>
        public bool IsPlayerSpeaking
        {
            get => m_IsPlayerSpeaking;
            set
            {
                if (m_IsPlayerSpeaking == value)
                    return;
                m_IsPlayerSpeaking = value;
                if (m_IsPlayerSpeaking)
                    OnPlayerStartSpeaking?.Invoke();
                else
                    OnPlayerStopSpeaking?.Invoke();
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
        }
        /// <summary>
        /// Unity's official microphone module starts recording, will trigger OnRecordingStart event.
        /// </summary>
        public void StartRecording()
        {
            if (m_IsCapturing)
                return;
#if UNITY_WEBGL && !UNITY_EDITOR
            m_LastPosition = WebGLGetPosition();
#else
            m_LastPosition = Microphone.GetPosition(m_DeviceName);
#endif
            m_IsCapturing = true;
            OnRecordingStart?.Invoke();
        }
        /// <summary>
        /// Unity's official microphone module stops recording, will trigger OnRecordingEnd event.
        /// </summary>
        public void StopRecording()
        {
            if (!m_IsCapturing)
                return;
            m_AudioToPush.Clear();
            m_IsCapturing = false;
            OnRecordingEnd?.Invoke();
        }
        /// <summary>
        /// Manually push the audio wave data to server.
        /// </summary>
        public void PushAudio()
        {
            foreach (string audioData in m_AudioToPush)
            {
                InworldController.Instance.SendAudio(audioData);
            }
            m_AudioToPush.Clear();
        }
        public virtual void StopAudio() => m_CurrentAudioSession.StopAudio();
        public virtual void StartAudio(List<string> characters = null)
        {
            if (characters == null || characters.Count == 0)
            {
                if (InworldController.CharacterHandler.CurrentCharacter)
                    m_CurrentAudioSession.StartAudio(new List<string>{InworldController.CurrentCharacter.BrainName});
                else if (InworldController.CharacterHandler.CurrentCharacterNames.Count != 0)
                    m_CurrentAudioSession.StartAudio(InworldController.CharacterHandler.CurrentCharacterNames);
            }
            else
                m_CurrentAudioSession.StartAudio(characters);
        }

        /// <summary>
        ///     Recalculate the background noise (including bg music, etc)
        ///     Please call it whenever audio environment changed in your game.
        /// </summary>
        public virtual void Calibrate() => m_BackgroundNoise = 0;
        protected IEnumerator _Calibrate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (WebGLIsRecording() == 0)
                StartMicrophone(m_DeviceName);
#else
            if (!Microphone.IsRecording(m_DeviceName))
                StartMicrophone(m_DeviceName);
#endif
            while (m_BackgroundNoise == 0)
            {
                int nSize = GetAudioData();
                yield return new WaitForSecondsRealtime(0.1f);
                m_BackgroundNoise = CalculateAmplitude();
            }
        }
#endregion

#region MonoBehaviour Functions
        protected virtual void Awake()
        {
            Init();
        }
        
        protected virtual void OnEnable()
        {
            InworldController.Client.OnPacketSent += OnPacketSent;
            InworldController.CharacterHandler.OnCharacterListJoined += OnCharacterJoined;
            InworldController.CharacterHandler.OnCharacterListLeft += OnCharacterLeft;
#if UNITY_WEBGL && !UNITY_EDITOR
            StartWebMicrophone();
#else            
            m_AudioCoroutine = AudioCoroutine();
            StartCoroutine(m_AudioCoroutine);
#endif
        }
        protected virtual void OnDisable()
        {
            if (InworldController.Instance)
            {
                InworldController.Client.OnPacketSent -= OnPacketSent;
                InworldController.CharacterHandler.OnCharacterListJoined -= OnCharacterJoined;
                InworldController.CharacterHandler.OnCharacterListLeft -= OnCharacterLeft;
            }
            StopCoroutine(m_AudioCoroutine);
            StopRecording();
            StopMicrophone(m_DeviceName);
        }

        protected virtual void OnDestroy()
        {
            m_Devices.Clear();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLDispose();
            s_WebGLBuffer = null;
#endif
            StopRecording();
            StopMicrophone(m_DeviceName);
        }
#endregion

#region Protected Functions
        protected virtual void Init()
        {
            m_CurrentAudioSession = new AudioSessionInfo();
            m_BufferSize = m_BufferSeconds * k_SampleRate;
            m_ByteBuffer = new byte[m_BufferSize * k_Channel * k_SizeofInt16];
            m_InputBuffer = new float[m_BufferSize * k_Channel];
            m_LastSampleMode = m_SamplingMode;
#if UNITY_WEBGL && !UNITY_EDITOR
            s_WebGLBuffer = new float[m_BufferSize * k_Channel];
            WebGLInit(OnWebGLInitialized);
#endif
        }
        protected virtual void OnPacketSent(InworldPacket packet)
        {
            if (packet is not ControlPacket controlPacket)
                return;
            switch (controlPacket.Action)
            {
                case ControlType.AUDIO_SESSION_START:
                    m_IsCapturing = true;
                    break;
                case ControlType.AUDIO_SESSION_END:
                    m_IsCapturing = false;
                    break;
            }
        }
        protected virtual void OnCharacterJoined(InworldCharacter character)
        {
            if (!InworldController.CharacterHandler.CurrentCharacter) // Group Chat Mode
            {
                //m_CurrentAudioSession.StopAudio();
                m_CurrentAudioSession.StartAudio(InworldController.CharacterHandler.CurrentCharacterNames);
            }
            character.Event.onCharacterSelected.AddListener(OnCharacterSelected);
            character.Event.onCharacterDeselected.AddListener(OnCharacterDeselected);
        }
        protected virtual void OnCharacterLeft(InworldCharacter character)
        {
            if (!InworldController.CharacterHandler.CurrentCharacter) // Group Chat Mode
            {
                m_CurrentAudioSession.StartAudio(InworldController.CharacterHandler.CurrentCharacterNames);
            }
            character.Event.onCharacterSelected.RemoveListener(OnCharacterSelected);
            character.Event.onCharacterDeselected.RemoveListener(OnCharacterDeselected);
        }
        protected virtual void OnCharacterSelected(string brainName) => StartAudio();

        protected virtual void OnCharacterDeselected(string brainName) => StopAudio();

        protected virtual IEnumerator AudioCoroutine()
        {
            while (true)
            {
                yield return _Calibrate();
                if (!m_IsCapturing || IsBlocked)
                {
                    yield return null;
                    continue;
                }
                yield return Collect();
            }
        }
        protected virtual IEnumerator Collect()
        {
            if (m_SamplingMode == MicSampleMode.NO_MIC)
                yield break;

            if (m_SamplingMode != MicSampleMode.PUSH_TO_TALK && m_BackgroundNoise == 0)
                yield break;

            int nSize = GetAudioData();
            if (nSize <= 0)
                yield break;
            IsPlayerSpeaking = CalculateAmplitude() > m_BackgroundNoise * m_PlayerVolumeThreshold;
            byte[] output = Output(nSize * m_Recording.channels);
            string audioData = Convert.ToBase64String(output);
            if (AutoPush)
                InworldController.Instance.SendAudio(audioData);
            else
                m_AudioToPush.Add(audioData);
            yield return new WaitForSecondsRealtime(0.1f);
        }
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
            if (!WebGLGetAudioData(m_LastPosition))
                return -1;
#else
            if (!m_Recording.GetData(m_InputBuffer, m_LastPosition))
                return -1;
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
        protected bool WebGLGetAudioData(int position)
        {
            if (m_InputBuffer == null || m_InputBuffer.Length == 0)
                return false;
            if (s_WebGLBuffer == null || s_WebGLBuffer.Length == 0)
                return false;
            for (int j = 0, i = position; i < s_WebGLBuffer.Length; j++, i++)
            {
                m_InputBuffer[j] = s_WebGLBuffer[i];
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
            WavUtility.ConvertAudioClipDataToInt16ByteArray(m_InputBuffer, nSize * m_Recording.channels, m_ByteBuffer);
            int nWavCount = nSize * m_Recording.channels * k_SizeofInt16;
            byte[] output = new byte[nWavCount];
            Buffer.BlockCopy(m_ByteBuffer, 0, output, 0, nWavCount);
            return output;
        }
        // Helper method to calculate the amplitude of audio data
        protected float CalculateAmplitude()
        {
            float fAvg = 0;
            int nCount = 0;
            foreach (float t in m_InputBuffer)
            {
                float tmp = Mathf.Abs(t);
                if (tmp == 0)
                    continue;
                fAvg += tmp;
                nCount++;
            }
            return nCount == 0 ? 0 : fAvg / nCount;
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
            if (m_Recording)
                Destroy(m_Recording);
            m_Recording = AudioClip.Create("Microphone", k_SampleRate * m_BufferSeconds, 1, k_SampleRate, false);
            if (s_WebGLBuffer == null || s_WebGLBuffer.Length == 0)
                s_WebGLBuffer = new float[k_SampleRate];
            WebGLInitSamplesMemoryData(s_WebGLBuffer, s_WebGLBuffer.Length);
            WebGLMicStart(microphoneDeviceIDFromName, k_SampleRate, m_BufferSeconds);
#else
            m_Recording = Microphone.Start(deviceName, true, m_BufferSeconds, k_SampleRate);
#endif
        }
        protected void StopMicrophone(string deviceName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLMicEnd();
            m_Recording.SetData(m_InputBuffer, 0);
#else
            Microphone.End(deviceName);
#endif
        }
#endregion
    }
}
