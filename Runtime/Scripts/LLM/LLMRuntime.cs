﻿/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.LLM.ModelConfig;
using Inworld.LLM.ModelServing;
using Inworld.LLM.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;


namespace Inworld.LLM
{
	public class LLMRuntime : MonoBehaviour
	{

		[SerializeField] protected int m_MaxChatHistorySize = 100;
		protected ServingID m_ServingID;
		protected TextGenerationConfig m_TextGenerationConfig;
		
		public List<Message> ChatHistory { get; }  = new List<Message>();
		public UnityEvent onChatHistoryUpdated;

		void Awake()
		{

		}
		protected void OnEnable()
		{
			if (!InworldController.Instance)
				return;
			InworldController.Instance.OnControllerStatusChanged += OnControllerStatusChanged;
		}
		protected void OnDisable()
		{
			if (!InworldController.Instance)
				return;
			InworldController.Instance.OnControllerStatusChanged -= OnControllerStatusChanged;
		}
		void Update()
		{
			if (ChatHistory.Count > m_MaxChatHistorySize)
				ChatHistory.RemoveAt(0);
		}
		void OnControllerStatusChanged(InworldConnectionStatus status, string detail)
		{
			if (status != InworldConnectionStatus.Initialized)
				return;
			// TODO(Yan): Replace servingID and textCfg with user's personal data. 
			if (m_ServingID == null)
				m_ServingID = new ServingID();
			if (m_TextGenerationConfig == null)
				m_TextGenerationConfig = new TextGenerationConfig();
		}
		/// <summary>
		/// Send Text to LLM Service
		/// </summary>
		/// <param name="text"></param>
		public bool SendText(string text)
		{
			if (InworldController.Status != InworldConnectionStatus.Initialized && InworldController.Status != InworldConnectionStatus.Connected)
				return false;
			ChatHistory.Add(MessageFactory.CreateRequest(text));
			onChatHistoryUpdated?.Invoke();
			UnityWebRequest uwr = new UnityWebRequest(InworldController.Server.CompleteChatURL, "POST");
			string jsonData = new CompleteChatRequest(m_ServingID, ChatHistory, m_TextGenerationConfig).ToJson;
			byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
			uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
			uwr.downloadHandler = new DownloadHandlerBuffer();
			uwr.timeout = 60;
			uwr.SetRequestHeader("Authorization", $"Bearer {InworldController.Token}");
			UnityWebRequestAsyncOperation updateRequest = uwr.SendWebRequest();
			updateRequest.completed += OnChatCompleted;
			return true;
		}

		void OnChatCompleted(AsyncOperation obj)
		{
			UnityWebRequest uwr = GetResponse(obj);
			if (uwr.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(uwr.error);
				Debug.Log(uwr.downloadHandler.text);
				return;
			}
			NetworkCompleteChatResponse response = JsonConvert.DeserializeObject<NetworkCompleteChatResponse>(uwr.downloadHandler.text);
			foreach (Choice choice in response.result.choices)
			{
				ChatHistory.Add(MessageFactory.CreateResponse(choice.message.content)); 
			}
			onChatHistoryUpdated?.Invoke();
		}
		public static UnityWebRequest GetResponse(AsyncOperation op) => op is UnityWebRequestAsyncOperation webTask ? webTask.webRequest : null;
	}
}