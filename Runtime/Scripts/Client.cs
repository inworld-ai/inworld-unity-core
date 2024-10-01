/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace Inworld
{
	public abstract class Client : MonoBehaviour
	{
		
#region Inspector Variables
		[SerializeField] protected InworldServerConfig m_ServerConfig;
		[SerializeField] protected string m_APIKey;
		[SerializeField] protected string m_APISecret;
		[SerializeField] protected AdditionalClientCfg m_Advanced;
#endregion

#region Events
		public event Action<InworldConnectionStatus> OnStatusChanged;
		public event Action<InworldError> OnErrorReceived;
#endregion

#region Protected Variables
		protected InworldConnectionStatus m_Status;
		protected IEnumerator m_OutgoingCoroutine;
		protected InworldError m_Error;
		protected Token m_Token;
#endregion

#region Properties
		/// <summary>
		/// Gets/Sets the current Inworld server this client is connecting.
		/// </summary>
		public InworldServerConfig Server
		{
			get => m_ServerConfig;
			internal set => m_ServerConfig = value;
		}
		/// <summary>
		/// Gets/Sets the token used to login Runtime server of Inworld.
		/// </summary>
		public Token Token
		{
			get => m_Token;
			set => m_Token = value;
		}
		/// <summary>
		/// Gets if the current token is valid.
		/// </summary>
		public virtual bool IsTokenValid => m_Token != null && m_Token.IsValid;
		/// <summary>
		/// Gets/Sets the current status of Inworld client.
		/// If set, it'll invoke OnStatusChanged events.
		/// </summary>
		public virtual InworldConnectionStatus Status
		{
			get => m_Status;
			set
			{
				if (m_Status == value)
					return;
				m_Status = value;
				OnStatusChanged?.Invoke(value);
			}
		}
		/// <summary>
		/// Gets/Sets the error message.
		/// </summary>
		public virtual string ErrorMessage
		{
			get => m_Error?.message ?? "";
			protected set
			{
				if (string.IsNullOrEmpty(value))
				{
					m_Error = null;
					return;
				}
				Error = new InworldError(value);
			}
		}
		/// <summary>
		/// Gets/Sets the error.
		/// If the error is no retry, it'll also set the status of this client to be error.
		/// </summary>
		public virtual InworldError Error
		{
			get => m_Error;
			set
			{
				m_Error = value;
				if (m_Error == null || !m_Error.IsValid)
				{
					return;
				}
				InworldAI.LogError(m_Error.message);
				OnErrorReceived?.Invoke(m_Error);
				Status = InworldConnectionStatus.Error; 
			}
		}
		/// <summary>
		/// Get/Set the public workspace.
		/// </summary>
		public string PublicWorkspace
		{
			get => m_Advanced?.PublicWorkspace;
			set
			{
				if (m_Advanced != null)
					m_Advanced.PublicWorkspace = value;
			}
		}
		/// <summary>
		/// Get/Set the custom token.
		/// </summary>
		public string CustomToken
		{
			get => m_Advanced?.CustomToken;
			set
			{
				if (m_Advanced != null)
					m_Advanced.CustomToken = value;
			}
		}
		/// <summary>
		/// Get/Set the game session ID.
		/// </summary>
		public string GameSessionID
		{
			get
			{
				if (m_Advanced == null)
					return null;
				if (string.IsNullOrEmpty(m_Advanced.GameSessionID))
					m_Advanced.GameSessionID = Token.sessionId;
				return m_Advanced.GameSessionID;
			}
			set
			{
				if (m_Advanced != null)
					m_Advanced.GameSessionID = value;
			}
		}
		/// <summary>
		/// Get/Set the API key.
		/// </summary>
		public string APIKey
		{
			get => m_APIKey;
			set => m_APIKey = value;
		}
		/// <summary>
		/// Get/Set the API Secret.
		/// </summary>
		public string APISecret
		{
			get => m_APISecret;
			set => m_APISecret = value;
		}
#endregion

#region Private Functions
		protected IEnumerator _GetAccessToken()
        {
            Status = InworldConnectionStatus.Initializing;
            string responseJson = CustomToken;
            if (string.IsNullOrEmpty(responseJson))
            {
                if (string.IsNullOrEmpty(m_APIKey))
                {
                    ErrorMessage = "Please fill API Key!";
                    yield break;
                }
                if (string.IsNullOrEmpty(m_APISecret))
                {
                    ErrorMessage = "Please fill API Secret!";
                    yield break;
                }
                string header = InworldAuth.GetHeader(m_ServerConfig.runtime, m_APIKey, m_APISecret);
                UnityWebRequest uwr = new UnityWebRequest(m_ServerConfig.TokenServer, "POST");
                Status = InworldConnectionStatus.Initializing;

                uwr.SetRequestHeader("Authorization", header);
                uwr.SetRequestHeader("Content-Type", "application/json");

                AccessTokenRequest req = new AccessTokenRequest
                {
                    api_key = m_APIKey,
                    resource_id = PublicWorkspace
                };
                string json = JsonUtility.ToJson(req);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    ErrorMessage = $"Error Get Token: {uwr.error}";
                }
                uwr.uploadHandler.Dispose();
                responseJson = uwr.downloadHandler.text;
            }
            m_Token = JsonUtility.FromJson<Token>(responseJson);
            if (!IsTokenValid)
            {
                ErrorMessage = "Get Token Failed";
                yield break;
            }
            Status = InworldConnectionStatus.Initialized;
        }
#endregion

		/// <summary>
		/// Use the input json string of token instead of API key/secret to load scene.
		/// This token can be fetched by other applications such as InworldWebSDK.
		/// </summary>
		/// <param name="token">the custom token to init.</param>
		public virtual bool InitWithCustomToken(string token)
		{
			m_Token = JsonUtility.FromJson<Token>(token);
			if (!IsTokenValid)
			{
				ErrorMessage = "Get Token Failed";
				return false;
			}
			Status = InworldConnectionStatus.Initialized;
			return true;
		}
		/// <summary>
		/// Gets the access token. Would be implemented by child class.
		/// </summary>
		public virtual void GetAccessToken() => StartCoroutine(_GetAccessToken());

		/// <summary>
		/// Send a text message.
		/// In LLM, characterID and immediate has to be null.
		/// In Character integration,
		/// characterID can be either character's BrainName, or its agent ID.
		/// 
		/// </summary>
		/// <param name="textToSend">text to send.</param>
		/// <param name="characterID">
		///		In LLM, characterID has to be null.
		///		In Character integration,
		///		characterID can be either character's BrainName, or its agent ID, or null.
		///		If null, it'll be sent as broadcasting.
		/// </param>
		/// <param name="immediate">
		///		In LLM, immediate is not related.
		///		In Character integration,
		///		if it's true, you have to send it 
		///		if it's false, it'll be put in a queue. We'll help you automatically connect Inworld server.
		/// </param>
		public abstract bool SendText(string textToSend, string characterID = null, bool immediate = false);
	}
}
