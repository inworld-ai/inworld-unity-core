/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Inworld.Entities;
using UnityEngine;

namespace Inworld.Audio
{
    public class WebGLCaptureModule : AudioCaptureModule, ICollectAudioHandler
    {
        public delegate void NativeCommand(string json);
        [DllImport("__Internal")] public static extern int WebGLInit(NativeCommand handler);
        [DllImport("__Internal")] public static extern int WebGLInitSamplesMemoryData(float[] array, int length);
        [DllImport("__Internal")] public static extern int WebGLIsRecording();
        [DllImport("__Internal")] public static extern string WebGLGetDeviceData();
        [DllImport("__Internal")] public static extern string WebGLGetDeviceCaps();
        [DllImport("__Internal")] public static extern int WebGLGetPosition();
        [DllImport("__Internal")] public static extern void WebGLMicStart(string deviceId, int frequency, int lengthSec);
        [DllImport("__Internal")] public static extern void WebGLMicEnd();
        [DllImport("__Internal")] public static extern void WebGLDispose();
        [DllImport("__Internal")] public static extern int WebGLIsPermitted();
        
        protected static float[] s_WebGLBuffer;
        public static bool WebGLPermission { get; set; }
        protected List<AudioDevice> m_Devices = new List<AudioDevice>();


        public override bool IsMicRecording => WebGLIsRecording() != 0;

        string GetWebGLMicDeviceID() => m_Devices.FirstOrDefault(d => d.label == Audio.DeviceName)?.deviceId;
        public override bool StartMicrophone()
        {
            string micDeviceID = GetWebGLMicDeviceID();
            if (string.IsNullOrEmpty(micDeviceID))
                throw new ArgumentException($"Couldn't acquire device ID for device name: {Audio.DeviceName} ");
            if (WebGLIsRecording() == 1)
                return false;
            if (Audio.RecordingClip)
                Destroy(Audio.RecordingClip);
            Audio.RecordingClip = AudioClip.Create("Microphone", k_InputSampleRate * k_InputBufferSecond, 1, k_InputSampleRate, false);
            if (s_WebGLBuffer == null || s_WebGLBuffer.Length == 0)
                s_WebGLBuffer = new float[k_InputSampleRate];
            WebGLInitSamplesMemoryData(s_WebGLBuffer, s_WebGLBuffer.Length);
            WebGLMicStart(micDeviceID, k_InputSampleRate, k_InputBufferSecond);
            Audio.ResetPointer();
            return true;
        }

        public override bool ChangeInputDevice(string deviceName)
        {
            throw new NotImplementedException();
        }

        public override bool StopMicrophone()
        {
            WebGLMicEnd();
            Audio.InputBuffer.Clear();
            return true;
        }

        public int OnCollectAudio()
        {
            throw new System.NotImplementedException();
        }

        public void ResetPointer()
        {
            throw new NotImplementedException();
        }
    }
}