/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using Inworld.Entities;
using Inworld.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityWebSocket;

namespace Inworld
{
    public class InworldClient : MonoBehaviour
    {
#region Inspector Variables
        [SerializeField] protected InworldServerConfig m_ServerConfig;
        [SerializeField] protected string m_SceneFullName;
        [SerializeField] protected string m_APIKey;
        [SerializeField] protected string m_APISecret;
        [SerializeField] protected string m_CustomToken;
        [SerializeField] protected string m_PublicWorkspace;
        [Tooltip("If checked, we'll automatically find the first scene belonged to the characters.")] 
        [SerializeField] protected bool m_AutoScene = false;
        [Tooltip("This is for the new previous data")] 
        [SerializeField] protected Continuation m_Continuation;
        [SerializeField] protected int m_MaxWaitingListSize = 100;
#endregion

#region Events
        public event Action<InworldConnectionStatus> OnStatusChanged;
        public event Action<InworldError> OnErrorReceived;
        public event Action<InworldPacket> OnPacketSent;
        public event Action<InworldPacket> OnGlobalPacketReceived;
        public event Action<InworldPacket> OnPacketReceived;
#endregion

#region Private variables
        const string k_NotImplemented = "No InworldClient found. Need at least one connection protocol";
        // These data will always be updated once session is refreshed and character ID is fetched. 
        // key by character's brain ID. Value contains its live session ID.
        protected readonly Dictionary<string, InworldCharacterData> m_LiveSessionData = new Dictionary<string, InworldCharacterData>();
        protected readonly Dictionary<string, Feedback> m_Feedbacks = new Dictionary<string, Feedback>();
        protected readonly IndexQueue<OutgoingPacket> m_Prepared = new IndexQueue<OutgoingPacket>();
        protected readonly IndexQueue<OutgoingPacket> m_Sent = new IndexQueue<OutgoingPacket>();
        protected WebSocket m_Socket;
        protected LoadSceneResponse m_CurrentSceneData;
        protected const string k_DisconnectMsg = "The remote party closed the WebSocket connection without completing the close handshake.";
        protected Token m_Token;
        protected IEnumerator m_OutgoingCoroutine;
        protected InworldConnectionStatus m_Status;
        protected InworldError m_Error;
#endregion

#region Properties
        /// <summary>
        /// Gets the live session data.
        /// key by character's full name (aka brainName) value by its agent ID.
        /// </summary>
        public Dictionary<string, InworldCharacterData> LiveSessionData => m_LiveSessionData;

        /// <summary>
        /// Get/Set the session history.
        /// </summary>
        public string SessionHistory { get; set; }
        /// <summary>
        /// Get/Set if client will automatically search for a scene for the selected characters.
        /// </summary>
        public bool AutoSceneSearch
        {
            get => m_AutoScene;
            set => m_AutoScene = value;
        }
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
        public string CurrentScene
        {
            get => m_SceneFullName;
            set => m_SceneFullName = value;
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
                m_Error = new InworldError(value);
                InworldAI.LogError(m_Error.message);
                OnErrorReceived?.Invoke(m_Error);
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
                if (m_Error.RetryType == ReconnectionType.NO_RETRY)
                    Status = InworldConnectionStatus.Error; 
            }
        }
        internal string APIKey
        {
            get => m_APIKey;
            set => m_APIKey = value;
        }
        internal string APISecret
        {
            get => m_APISecret;
            set => m_APISecret = value;
        }
        internal string SceneFullName
        {
            get => m_SceneFullName;
            set => m_SceneFullName = value;
        }
        internal string CustomToken
        {
            get => m_CustomToken;
            set => m_CustomToken = value;
        }
#endregion

#region Unity LifeCycle
        protected virtual void OnEnable()
        {
            m_OutgoingCoroutine = OutgoingCoroutine();
            StartCoroutine(m_OutgoingCoroutine);
        }
        void OnDestroy()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            var webSocketManager = FindObjectOfType<WebSocketManager>();
            if(webSocketManager)
                Destroy(webSocketManager.gameObject);
#endif
        }
#endregion

#region APIs
        /// <summary>
        /// Get the InworldCharacterData by characters' full name.
        /// </summary>
        /// <param name="characterFullNames">the request characters' Brain ID.</param>
        public Dictionary<string, string> GetCharacterDataByFullName(List<string> characterFullNames)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (characterFullNames == null || characterFullNames.Count == 0)
                return result;
            foreach (string brainID in characterFullNames)
            {
                if (m_LiveSessionData.TryGetValue(brainID, out InworldCharacterData value))
                    result[brainID] = value.agentId;
                else
                    result[brainID] = "";
            }
            return result;
        }
        /// <summary>
        /// Gets the InworldCharacterData by the given agentID.
        /// Usually used when processing packets, but don't know it's sender/receiver of characters.
        /// </summary>
        public InworldCharacterData GetCharacterDataByID(string agentID) => 
            LiveSessionData.Values.FirstOrDefault(c => !string.IsNullOrEmpty(agentID) && c.agentId == agentID);
        /// <summary>
        /// Gets the scene name by the given target characters.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSceneNameByCharacter()
        {
            if (m_Prepared.Count == 0)
                return null;
            List<string> characterFullNames = m_Prepared[0].Targets.Keys.ToList();
            List<string> result = new List<string>();
            foreach (InworldWorkspaceData wsData in InworldAI.User.Workspace)
            {
                string output = wsData.GetSceneNameByCharacters(characterFullNames);
                if (!string.IsNullOrEmpty(output))
                {
                    result.Add(output); // Currently, we can only support loading 1 scene per session.
                    return result;
                }
            }
            return characterFullNames;
        }
        /// <summary>
        /// Prepare the session. If the session is freshly established. Please call this.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator PrepareSession()
        {
            SendCapabilities();
            yield return null;
            SendSessionConfig();
            yield return null;
            SendClientConfig();
            yield return null;
            SendUserConfig();
            yield return null;
            SendHistory();
            yield return null;
            LoadScene(m_SceneFullName);
        }
        /// <summary>
        /// Send Feedback data to server.
        /// </summary>
        /// <param name="interactionID">The feedback bubble's interactionID</param>
        /// <param name="correlationID">The feedback bubble's correlationID</param>
        /// <param name="feedback">The actual feedback content</param>
        public virtual void SendFeedbackAsync(string interactionID, string correlationID, Feedback feedback)
        {
            StartCoroutine(_SendFeedBack( interactionID, correlationID, feedback));
        }
        /// <summary>
        /// Get the session history data. Stored at property SessionHistory.
        /// By default, it'll be stored in the memory only, Please store it to your local storage for future use.
        /// </summary>
        /// <param name="sceneFullName">the related scene</param>
        public virtual void GetHistoryAsync(string sceneFullName) => StartCoroutine(_GetHistoryAsync(sceneFullName));
        /// <summary>
        /// Generally send packets.
        /// Will automatically be called in outgoing queue.
        /// 
        /// Can be called directly by API. 
        /// </summary>
        public virtual void SendPackets()
        {
            OutgoingPacket rawData = m_Prepared.Dequeue(true);
            if (rawData == null)
                return;
            m_Socket.SendAsync(rawData.RawPacket.ToJson);
            m_Sent.Enqueue(rawData);
        }
        /// <summary>
        /// Gets the access token. Would be implemented by child class.
        /// </summary>
        public virtual void GetAccessToken() => StartCoroutine(_GetAccessToken(m_PublicWorkspace));
        /// <summary>
        /// Reconnect session or start a new session if the current session is invalid.
        /// </summary>
        public void Reconnect() 
        {
            if (IsTokenValid)
                StartSession();
            else
                GetAccessToken();
        }
        /// <summary>
        /// Gets the live session info once load scene completed.
        /// The returned LoadSceneResponse contains the session ID and all the live session ID for each InworldCharacters in this InworldScene.
        /// </summary>
        public virtual LoadSceneResponse GetLiveSessionInfo() => m_CurrentSceneData;
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
        /// Start the session by the session ID.
        /// </summary>
        public virtual void StartSession() => StartCoroutine(_StartSession());
        /// <summary>
        /// Disconnect Inworld Server.
        /// </summary>
        public virtual void Disconnect() => StartCoroutine(DisconnectAsync());
        public virtual void UnloadScene()
        {
            foreach (KeyValuePair<string, InworldCharacterData> data in m_LiveSessionData)
            {
                data.Value.agentId = "";
            }
        }
        /// <summary>
        /// Send LoadScene request to Inworld Server.
        /// </summary>
        /// <param name="sceneFullName">the full string of the scene to load.</param>
        public virtual void LoadScene(string sceneFullName = "")
        {
            UnloadScene();
            InworldAI.LogEvent("Login_Runtime");
            if (!string.IsNullOrEmpty(sceneFullName))
            {
                InworldAI.Log($"Load Scene: {sceneFullName}");
                m_SceneFullName = sceneFullName;
                m_Socket.SendAsync(new LoadScenePacket(m_SceneFullName).ToJson);
            }
            else
            {
                List<string> result = AutoSceneSearch ? GetSceneNameByCharacter() : m_Prepared[0].Targets.Keys.ToList();
                if (result == null || result.Count == 0)
                {
                    InworldAI.LogException("Characters not found in the workspace");
                    return;
                }
                if (result.Count == 1 && result[0].Split("/scenes/").Length > 0)
                {
                    m_SceneFullName = result[0];
                    InworldAI.Log($"Load Scene: {m_SceneFullName}");
                    m_Socket.SendAsync(new LoadScenePacket(m_SceneFullName).ToJson);
                }
                else
                {
                    InworldAI.Log($"Load Characters directly.");
                    m_Socket.SendAsync(new LoadCharactersPacket(result).ToJson);
                }
            }
        }
        /// <summary>
        /// Send Capabilities to Inworld Server.
        /// It should be sent immediately after session started to enable all the conversations. 
        /// </summary>
        public virtual void SendCapabilities()
        {
            string jsonToSend = JsonUtility.ToJson(InworldAI.Capabilities.ToPacket);
            InworldAI.Log($"Sending Capabilities: {InworldAI.Capabilities}");
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// Send Session Config to Inworld Server.
        /// It should be sent right after sending Capabilities. 
        /// </summary>
        public virtual void SendSessionConfig()
        {
            string gameSessionID = $"{InworldAI.User.Name}:{m_Token.sessionId}:{_GetSessionGUID()}";
            string jsonToSend = JsonUtility.ToJson(m_Token.ToPacket()); 
            InworldAI.Log($"Sending Session Info."); 
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// Send Client Config to Inworld Server.
        /// It should be sent right after sending Session Config. 
        /// </summary>
        public virtual void SendClientConfig()
        {
            string jsonToSend = JsonUtility.ToJson(InworldAI.UnitySDK.ToPacket);
            InworldAI.Log($"Sending Client Info: {InworldAI.UnitySDK}");
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// Send User Config to Inworld Server.
        /// It should be sent right after sending Client Config. 
        /// </summary>
        public virtual void SendUserConfig()
        {
            string jsonToSend = JsonUtility.ToJson(InworldAI.User.Request.ToPacket);
            InworldAI.Log($"Sending User Config: {InworldAI.User.Request}");
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// Send the previous dialog (New version) to specific scene.
        /// Can be supported by either previous state (base64) or previous dialog (actor: text)
        /// </summary>
        /// <param name="sceneFullName">target scene to send</param>
        public virtual void SendHistory()
        {
            if (!m_Continuation.IsValid)
            {
                if (string.IsNullOrEmpty(SessionHistory))
                    return;
                m_Continuation.continuationType = ContinuationType.CONTINUATION_TYPE_EXTERNALLY_SAVED_STATE;
                m_Continuation.externallySavedState = SessionHistory;
            }
            string jsonToSend = JsonUtility.ToJson(m_Continuation.ToPacket);
            InworldAI.Log($"Send previous history... {jsonToSend}");
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// New Send messages to an InworldCharacter in this current scene.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="textToSend">the message to send.</param>
        /// <param name="characters">the list of the characters full name.</param>
        public virtual void SendTextTo(string textToSend, List<string> characters = null)
        {
            if (string.IsNullOrEmpty(textToSend))
                return;
            Dictionary<string, string> characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            OutgoingPacket rawData = new OutgoingPacket(new TextEvent(textToSend), characterToReceive);
            m_Prepared.Enqueue(rawData);
            OnPacketSent?.Invoke(rawData.RawPacket);
        }
        /// <summary>
        /// Legacy Send messages to an InworldCharacter in this current scene.
        /// </summary>
        /// <param name="characterID">the live session ID of the single character to send</param>
        /// <param name="textToSend">the message to send.</param>
        public virtual void SendText(string characterID, string textToSend)
        {
            if (string.IsNullOrEmpty(characterID) || string.IsNullOrEmpty(textToSend))
                return;
            InworldPacket packet = new TextPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "TEXT",
                packetId = new PacketId(),
                routing = new Routing(characterID),
                text = new TextEvent(textToSend)
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            OnPacketSent?.Invoke(packet);
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// New Send narrative action to an InworldCharacter in this current scene.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="narrativeAction">the narrative action to send.</param>
        /// <param name="characters">the list of the characters full name.</param>
        public virtual void SendNarrativeActionTo(string narrativeAction, List<string> characters = null)
        {
            if (string.IsNullOrEmpty(narrativeAction))
                return;
            Dictionary<string, string> characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            OutgoingPacket rawData = new OutgoingPacket(new ActionEvent(narrativeAction), characterToReceive);
            m_Prepared.Enqueue(rawData);
            OnPacketSent?.Invoke(rawData.RawPacket);
        }
        /// <summary>
        /// Legacy Send a narrative action to an InworldCharacter in this current scene.
        /// </summary>
        /// <param name="characterID">the live session ID of the character to send</param>
        /// <param name="narrativeAction">the narrative action to send.</param>
        public virtual void SendNarrativeAction(string characterID, string narrativeAction)
        {
            if (string.IsNullOrEmpty(characterID) || string.IsNullOrEmpty(narrativeAction))
                return;
            InworldPacket packet = new ActionPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "ACTION",
                packetId = new PacketId(),
                routing = new Routing(characterID),
                action = new ActionEvent
                {
                    narratedAction = new NarrativeAction
                    {
                        content = narrativeAction
                    }
                }
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            OnPacketSent?.Invoke(packet);
            m_Socket.SendAsync(jsonToSend);
        }
                /// <summary>
        /// New Send the CancelResponse Event to InworldServer to interrupt the character's speaking.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="interactionID">the handle of the dialog context that needs to be cancelled.</param>
        /// <param name="utteranceID">the current utterance ID that needs to be cancelled.</param>
        /// <param name="characters">the full name of the characters in the scene.</param>
        public virtual void SendCancelEventTo(string interactionID, string utteranceID = "", List<string> characters = null)
        {
            if (string.IsNullOrEmpty(interactionID))
                return;
            Dictionary<string, string>  characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            MutationEvent mutation = new MutationEvent
            {
                cancelResponses = new CancelResponse
                {
                    interactionId = interactionID,
                    utteranceId = new List<string> {utteranceID}
                }
            };
            m_Prepared.Enqueue(new OutgoingPacket(mutation, characterToReceive));
        }
        /// <summary>
        /// Legacy Send the CancelResponse Event to InworldServer to interrupt the character's speaking.
        /// </summary>
        /// <param name="characterID">the live session ID of the character to send</param>
        /// <param name="utteranceID">the current utterance ID that needs to be cancelled.</param>
        /// <param name="interactionID">the handle of the dialog context that needs to be cancelled.</param>
        public virtual void SendCancelEvent(string characterID, string interactionID, string utteranceID = "")
        {
            if (string.IsNullOrEmpty(characterID))
                return;
            MutationPacket cancelPacket = new MutationPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "CANCEL_RESPONSE",
                packetId = new PacketId(),
                routing = new Routing(characterID),
                mutation = new MutationEvent
                {
                    cancelResponses = new CancelResponse
                    {
                        interactionId = interactionID,
                        utteranceId = new List<string> { utteranceID }
                    }
                }
            };
            OnPacketSent?.Invoke(cancelPacket); 
            m_Socket.SendAsync(JsonUtility.ToJson(cancelPacket));
        }
        /// <summary>
        /// New Send the trigger to an InworldCharacter in the current scene.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="triggerName">the name of the trigger to send.</param>
        /// <param name="parameters">the parameters and their values for the triggers.</param>
        /// <param name="characters">the full name of the characters in the scene.</param>
        public virtual void SendTriggerTo(string triggerName, Dictionary<string, string> parameters = null, List<string> characters = null)
        {
            if (string.IsNullOrEmpty(triggerName))
                return;
            Dictionary<string, string>  characterToReceive = GetCharacterDataByFullName(characters);
            // if (characterToReceive.Count == 0)
            //     return;
            m_Prepared.Enqueue(new OutgoingPacket(new CustomEvent(triggerName, parameters), characterToReceive));
        }
        /// <summary>
        /// Legacy Send the trigger to an InworldCharacter in the current scene.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send.</param>
        /// <param name="triggerName">the name of the trigger to send.</param>
        /// <param name="parameters">the parameters and their values for the triggers.</param>
        public virtual void SendTrigger(string charID, string triggerName, Dictionary<string, string> parameters = null)
        {
            if (string.IsNullOrEmpty(charID))
                return;
            InworldPacket packet = new CustomPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "CUSTOM",
                packetId = new PacketId(),
                routing = new Routing(charID),
                custom = new CustomEvent(triggerName, parameters)
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            InworldAI.Log($"Send Trigger {triggerName}");
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// New Send AUDIO_SESSION_START control events to server.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="characters">the full name of the characters to send.</param>
        public virtual void StartAudioTo(List<string> characters = null)
        {
            Dictionary<string, string>  characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            ControlEvent control = new ControlEvent
            {
                action = ControlType.AUDIO_SESSION_START.ToString()
            };
            OutgoingPacket rawData = new OutgoingPacket(control, characterToReceive);
            m_Prepared.Enqueue(rawData);
            OnPacketSent?.Invoke(rawData.RawPacket);
        }
        /// <summary>
        /// Legacy Send AUDIO_SESSION_START control events to server.
        /// Without sending this message, all the audio data would be discarded by server.
        /// However, if you send this event twice in a row, without sending `StopAudio()`, Inworld server will also through exceptions and terminate the session.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send.</param>
        public virtual void StartAudio(string charID)
        {
            if (string.IsNullOrEmpty(charID))
                return;

            InworldPacket packet = new ControlPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "TEXT",
                packetId = new PacketId(),
                routing = new Routing(charID),
                control = new ControlEvent
                {
                    action = ControlType.AUDIO_SESSION_START.ToString()
                }
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// New Send AUDIO_SESSION_END control events to server to.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// </summary>
        /// <param name="characters">the full name of the character to send.</param>
        public virtual void StopAudioTo(List<string> characters = null)
        {
            Dictionary<string, string>  characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            ControlEvent control = new ControlEvent
            {
                action = ControlType.AUDIO_SESSION_END.ToString()
            };
            OutgoingPacket output = new OutgoingPacket(control, characterToReceive);
            output.OnDequeue();
            if (!string.IsNullOrEmpty(output.RawPacket?.routing?.target?.name))
                m_Socket.SendAsync(output.RawPacket.ToJson); // Do not enqueue dataChunk, They can be discarded.
            OnPacketSent?.Invoke(output.RawPacket);
        }
        /// <summary>
        /// Legacy Send AUDIO_SESSION_END control events to server to.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send.</param>
        public virtual void StopAudio(string charID)
        {
            if (string.IsNullOrEmpty(charID))
                return;
            InworldPacket packet = new ControlPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "TEXT",
                packetId = new PacketId(),
                routing = new Routing(charID),
                control = new ControlEvent
                {
                    action = ControlType.AUDIO_SESSION_END.ToString()
                }
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            m_Socket.SendAsync(jsonToSend);
        }
        /// <summary>
        /// New Send the wav data to server to a specific character.
        /// Need to make sure that AUDIO_SESSION_START control event has been sent to server.
        /// NOTE: 1. New method uses brain ID (aka character's full name) instead of live session ID
        ///       2. New method support broadcasting to multiple characters.
        /// Only the base64 string of the wave data is supported by Inworld server.
        /// Additionally, the sample rate of the wave data has to be 16000, mono channel.
        /// </summary>
        /// <param name="base64">the base64 string of the wave data to send.</param>
        /// <param name="characters">the full name of the character to send.</param>
        public virtual void SendAudioTo(string base64, List<string> characters = null)
        {
            if (Status != InworldConnectionStatus.Connected)
                return;
            if (string.IsNullOrEmpty(base64))
                return;
            Dictionary<string, string>  characterToReceive = GetCharacterDataByFullName(characters);
            if (characterToReceive.Count == 0)
                return;
            DataChunk dataChunk = new DataChunk
            {
                type = "AUDIO",
                chunk = base64
            };
            OutgoingPacket output = new OutgoingPacket(dataChunk, characterToReceive);
            output.OnDequeue();
            if (!string.IsNullOrEmpty(output.RawPacket?.routing?.target?.name))
                m_Socket.SendAsync(output.RawPacket.ToJson); // Do not enqueue dataChunk, They can be discarded.
        }
        /// <summary>
        /// Legacy Send the wav data to server to a specific character.
        /// Need to make sure that AUDIO_SESSION_START control event has been sent to server.
        ///
        /// Only the base64 string of the wave data is supported by Inworld server.
        /// Additionally, the sample rate of the wave data has to be 16000, mono channel.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send.</param>
        /// <param name="base64">the base64 string of the wave data to send.</param>
        public virtual void SendAudio(string charID, string base64)
        {
            if (Status != InworldConnectionStatus.Connected)
                return;
            if (string.IsNullOrEmpty(charID))
                return;
            InworldPacket packet = new AudioPacket
            {
                timestamp = InworldDateTime.UtcNow,
                type = "AUDIO",
                packetId = new PacketId(),
                routing = new Routing(charID),
                dataChunk = new DataChunk
                {
                    type = "AUDIO",
                    chunk = base64
                }
            };
            string jsonToSend = JsonUtility.ToJson(packet);
            OnPacketSent?.Invoke(packet);
            m_Socket.SendAsync(jsonToSend);
        }
#endregion

#region Private Functions
        protected virtual IEnumerator OutgoingCoroutine()
        {
            while (true)
            {
                if (m_Prepared.Count > 0)
                {
                    if (Status == InworldConnectionStatus.Connected)
                    {
                        SendPackets();
                    }
                    if (Status == InworldConnectionStatus.Idle)
                    {
                        GetAccessToken();
                    }
                    if (Status == InworldConnectionStatus.Initialized)
                    {
                        StartSession();
                    }
                }
                if (m_Sent.Count > m_MaxWaitingListSize)
                    m_Sent.Dequeue();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        protected virtual void _RegisterLiveSession(LoadSceneResponse loadSceneResponse)
        {
            m_LiveSessionData.Clear();
            // YAN: Fetch all the characterData in the current session.
            foreach (InworldCharacterData agent in loadSceneResponse.agents.Where(agent => !string.IsNullOrEmpty(agent.agentId) && !string.IsNullOrEmpty(agent.brainName)))
            {
                m_LiveSessionData[agent.brainName] = agent;
            }
        }
        protected string _GetSessionFullName(string sceneFullName)
        {
            string[] data = sceneFullName.Split('/');
            return data.Length != 4 ? "" : $"workspaces/{data[1]}/sessions/{m_Token.sessionId}";
        }
        protected string _GetCallbackReference(string sessionFullName, string interactionID, string correlationID)
        {
            return $"{sessionFullName}/interactions/{interactionID}/groups/{correlationID}";
        }
        protected IEnumerator _GetAccessToken(string workspaceFullName = "")
        {
            Status = InworldConnectionStatus.Initializing;
            string responseJson = m_CustomToken;
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
                    resource_id = workspaceFullName
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
        protected IEnumerator _StartSession()
        {
            if (Status == InworldConnectionStatus.Connected)
                yield break;
            string url = m_ServerConfig.SessionURL(m_Token.sessionId);
            if (!IsTokenValid)
                yield break;
            string[] param = {m_Token.type, m_Token.token};
            m_Socket = WebSocketManager.GetWebSocket(url);
            if (m_Socket == null)
                m_Socket = new WebSocket(url, param);
            m_Socket.OnOpen += OnSocketOpen;
            m_Socket.OnMessage += OnMessageReceived;
            m_Socket.OnClose += OnSocketClosed;
            m_Socket.OnError += OnSocketError;
            Status = InworldConnectionStatus.Connecting;
            m_Socket.ConnectAsync();
        }
        void OnSocketOpen(object sender, OpenEventArgs e)
        {
            InworldAI.Log($"Connect {m_Token.sessionId}");
            StartCoroutine(PrepareSession());
        }
        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            NetworkPacketResponse response = JsonUtility.FromJson<NetworkPacketResponse>(e.Data);
            if (response == null || response.result == null)
            {
                ErrorMessage = e.Data;
                return;
            }
            if (response.error != null && !string.IsNullOrEmpty(response.error.message))
            {
                Error = response.error;
                return;
            }
            InworldNetworkPacket packetReceived = response.result;
            if (packetReceived.Type == PacketType.SESSION_RESPONSE)
            {
                if (packetReceived.Packet is SessionResponsePacket sessionResponse)
                {
                    m_CurrentSceneData = new LoadSceneResponse();
                    if (sessionResponse.sessionControlResponse?.loadedScene?.agents?.Count > 0)
                        m_CurrentSceneData.agents.AddRange(sessionResponse.sessionControlResponse.loadedScene.agents);
                    if (sessionResponse.sessionControlResponse?.loadedCharacters?.agents?.Count > 0)
                        m_CurrentSceneData.agents.AddRange(sessionResponse.sessionControlResponse.loadedCharacters.agents);
                    _RegisterLiveSession(m_CurrentSceneData);
                    Status = InworldConnectionStatus.Connected;
                }
                return;
            }
            if (packetReceived.Type == PacketType.CONTROL)
            {
                if (packetReceived.Packet is ControlPacket controlPacket)
                {
                    switch (controlPacket.Action)
                    {
                        case ControlType.WARNING:
                            InworldAI.LogWarning(packetReceived.control.description);
                            return;
                        case ControlType.INTERACTION_END:
                            _FinishInteraction(controlPacket.packetId.correlationId);
                            break;
                    }
                }
            }
            if (packetReceived.Packet.Source == SourceType.WORLD && packetReceived.Packet.Target == SourceType.PLAYER)
                OnGlobalPacketReceived?.Invoke(packetReceived.Packet);
            if (packetReceived.Type == PacketType.UNKNOWN)
            {
                InworldAI.LogWarning($"Received Unknown {e.Data}");
            }
            OnPacketReceived?.Invoke(packetReceived.Packet);
        }
        void OnSocketClosed(object sender, CloseEventArgs e)
        {
            InworldAI.Log($"Closed: StatusCode: {e.StatusCode}, Reason: {e.Reason}");
            // Status = e.StatusCode == CloseStatusCode.Normal ? InworldConnectionStatus.Idle : InworldConnectionStatus.LostConnect;
            Status = InworldConnectionStatus.Idle;
        }
        void OnSocketError(object sender, ErrorEventArgs e)
        {
            if (e.Message != k_DisconnectMsg)
                ErrorMessage = e.Message;
        }
        protected IEnumerator DisconnectAsync()
        {
            yield return new WaitForEndOfFrame();
            m_Socket?.CloseAsync();
            yield return new WaitForEndOfFrame();
        }
        protected string _GetSessionGUID()
        {
            if (!string.IsNullOrEmpty(m_Continuation?.externallySavedState) && m_Continuation?.externallySavedState.Length >= 8)
                return m_Continuation.externallySavedState[..8];
            if (!string.IsNullOrEmpty(SessionHistory) && SessionHistory.Length >= 8)
                return SessionHistory[..8];
            return Guid.NewGuid().ToString()[..8];
        }
        protected IEnumerator _GetHistoryAsync(string sceneFullName)
        {
            string sessionFullName = _GetSessionFullName(sceneFullName);
            UnityWebRequest uwr = new UnityWebRequest(m_ServerConfig.LoadSessionURL(sessionFullName), "GET");
            uwr.SetRequestHeader("Grpc-Metadata-session-id", m_Token.sessionId);
            uwr.SetRequestHeader("Authorization", $"Bearer {m_Token.token}");
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                ErrorMessage = $"Error loading scene {m_Token.sessionId}: {uwr.error} {uwr.downloadHandler.text}";
                uwr.uploadHandler.Dispose();
                yield break;
            }
            string responseJson = uwr.downloadHandler.text;
            PreviousSessionResponse response = JsonUtility.FromJson<PreviousSessionResponse>(responseJson);
            SessionHistory = response.state;
            InworldAI.Log($"Get Previous Content Encrypted: {SessionHistory}");
        }
        IEnumerator _SendFeedBack(string interactionID, string correlationID, Feedback feedback)
        {
            if (string.IsNullOrEmpty(interactionID))
            {
                InworldAI.LogError("No interaction ID for feedback");
                yield break;
            }
            if (m_Feedbacks.ContainsKey(interactionID))
                yield return _PatchFeedback(interactionID, correlationID, feedback); // Patch
            else
                yield return _PostFeedback(interactionID, correlationID, feedback);
        }
        IEnumerator _PostFeedback(string interactionID, string correlationID, Feedback feedback)
        {
            string sessionFullName = _GetSessionFullName(m_SceneFullName);
            string callbackRef = _GetCallbackReference(sessionFullName, interactionID, correlationID);
            UnityWebRequest uwr = new UnityWebRequest(m_ServerConfig.FeedbackURL(callbackRef), "POST");
            uwr.SetRequestHeader("Grpc-Metadata-session-id", m_Token.sessionId);
            uwr.SetRequestHeader("Authorization", $"Bearer {m_Token.token}");
            uwr.SetRequestHeader("Content-Type", "application/json");
            string json = JsonUtility.ToJson(feedback);
            Debug.Log($"SEND: {json}");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                ErrorMessage = $"Error Posting feedbacks {uwr.downloadHandler.text} Error: {uwr.error}";
                uwr.uploadHandler.Dispose();
                uwr.downloadHandler.Dispose();
                yield break;
            }
            string responseJson = uwr.downloadHandler.text;
            InworldAI.Log($"Received: {responseJson}");
        }
        IEnumerator _PatchFeedback(string interactionID, string correlationID, Feedback feedback)
        {
            yield return _PostFeedback(interactionID, correlationID, feedback); //TODO(Yan): Use Patch instead of Post for detailed json.
        }
        void _FinishInteraction(string correlationID)
        {
            List<OutgoingPacket> toRemove = new List<OutgoingPacket>();
            for (int i = 0; i < m_Sent.Count; i++)
            {
                if (m_Sent[i].ID == correlationID)
                    toRemove.Add(m_Sent[i]);
            }
            foreach (OutgoingPacket packet in toRemove)
            {
                m_Sent.Remove(packet);
            }
        }
#endregion
    }
}
