/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using AOT;
using Inworld.Audio;
using Inworld.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Inworld
{
	public class WebGLAudioCapture : AudioCapture
	{
		protected static float[] s_WebGLBuffer;
		public static bool WebGLPermission { get; set; }
		protected delegate void NativeCommand(string json);
		[DllImport("__Internal")] protected static extern int WebGLInit(NativeCommand handler);
		[DllImport("__Internal")] protected static extern int WebGLInitSamplesMemoryData(float[] array, int length);
		[DllImport("__Internal")] protected static extern int WebGLIsRecording();
		[DllImport("__Internal")] protected static extern string WebGLGetDeviceData();
		[DllImport("__Internal")] protected static extern string WebGLGetDeviceCaps();
		[DllImport("__Internal")] protected static extern int WebGLGetPosition();
		[DllImport("__Internal")] protected static extern void WebGLMicStart(string deviceId, int frequency, int lengthSec);
		[DllImport("__Internal")] protected static extern void WebGLMicEnd();
		[DllImport("__Internal")] protected static extern void WebGLDispose();
		[DllImport("__Internal")] protected static extern int WebGLIsPermitted();
		
		public override List<AudioDevice> Devices
		{
			get
			{

				if (m_Devices.Count == 0)
				{
					m_Devices = JsonUtility.FromJson<WebGLAudioDevicesData>(WebGLGetDeviceData()).devices;
				}
				return m_Devices;

			}
		}
		protected override void OnEnable()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
            StartWebMicrophone();
#else            
			m_AudioCoroutine = AudioCoroutine();
			StartCoroutine(m_AudioCoroutine);
#endif
		}
		protected override void OnDestroy()
		{
			m_Devices.Clear();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLDispose();
            s_WebGLBuffer = null;
#endif
			StopMicrophone(m_DeviceName);
		}
		public override void ChangeInputDevice(string deviceName)
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
		protected override IEnumerator _Calibrate()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
            if (WebGLIsRecording() == 0)
                StartMicrophone(m_DeviceName);
#else
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
		protected override void Init()
		{
			m_BufferSize = m_BufferSeconds * k_SampleRate;
			m_ByteBuffer = new byte[m_BufferSize * k_Channel * k_SizeofInt16];
			m_InputBuffer = new float[m_BufferSize * k_Channel];
			m_InitSampleMode = m_SamplingMode;
#if UNITY_WEBGL && !UNITY_EDITOR
            s_WebGLBuffer = new float[m_BufferSize * k_Channel];
            WebGLInit(OnWebGLInitialized);
#endif
		}
		public override void StartMicrophone(string deviceName)
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
		protected override void StopMicrophone(string deviceName)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLMicEnd();
            m_Recording.SetData(m_InputBuffer, 0);
#else
			Microphone.End(deviceName);
#endif
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
		
		public override void StartWebMicrophone()
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
		protected override int GetAudioData()
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
			if (!m_Recording || !m_Recording.GetData(m_InputBuffer, m_LastPosition))
				return -1;
#endif
			m_LastPosition = m_nPosition % m_BufferSize;
			return nSize;
		}
	}
}
