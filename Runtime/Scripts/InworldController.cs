/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Inworld
{
    /// <summary>
    /// The InworldController acts as an API hub within this Unity application, primarily designed for backward compatibility with previous versions.
    /// It serves as a central point for managing API interfaces, ensuring that the system maintains its interfaces with older versions
    /// while delegating the actual execution of each API call to subordinate scripts.
    /// </summary>
    [RequireComponent(typeof(InworldClient), typeof(AudioCapture), typeof(CharacterHandler))]
    public class InworldController : SingletonBehavior<InworldController>
    {
        [SerializeField] protected InworldGameData m_GameData;
        [SerializeField] protected string m_SceneFullName;
        
        protected InworldClient m_Client;
        protected AudioCapture m_AudioCapture;
        protected CharacterHandler m_CharacterHandler;
        
        /// <summary>
        /// Gets the AudioCapture of the InworldController.
        /// </summary>
        public static AudioCapture Audio
        {
            get
            {
                if (!Instance) 
                    return null;

                if (Instance.m_AudioCapture)
                    return Instance.m_AudioCapture;

                Instance.m_AudioCapture = Instance.GetComponent<AudioCapture>();
                return Instance.m_AudioCapture;
            }
        }
        /// <summary>
        /// Gets the CharacterHandler of the InworldController.
        /// </summary>
        public static CharacterHandler CharacterHandler
        {
            get
            {
                if (!Instance)
                    return null;
                if (Instance.m_CharacterHandler)
                    return Instance.m_CharacterHandler;
                Instance.m_CharacterHandler = Instance.GetComponent<CharacterHandler>();
                return Instance.m_CharacterHandler;
            }
        }
        /// <summary>
        /// Gets/Sets this InworldController's protocol client.
        /// </summary>
        public static InworldClient Client
        {
            get
            {
                if (!Instance) 
                    return null;

                if (Instance.m_Client)
                    return Instance.m_Client;

                Instance.m_Client = Instance.GetComponent<InworldClient>();
                return Instance.m_Client;
            }
            set
            {
                if (!Instance)
                    return;
                Instance.m_Client = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(Instance);
                AssetDatabase.SaveAssets();
#endif
            }
        }
        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        public static InworldConnectionStatus Status => Client.Status;
        /// <summary>
        /// Gets the current workspace's full name.
        /// </summary>
        public string CurrentWorkspace
        {
            get
            {
                string[] data = m_SceneFullName.Split(new[] { "/scenes/", "/characters/" }, StringSplitOptions.None);
                return data.Length > 1 ? data[0] : m_SceneFullName;
            }
        }
        /// <summary>
        /// Gets the current InworldScene's full name.
        /// </summary>
        public string CurrentScene => Client ? Client.CurrentScene : m_SceneFullName;
        /// <summary>
        /// Gets/Sets the current interacting character.
        /// </summary>
        public static InworldCharacter CurrentCharacter
        {
            get
            {
                if (CharacterHandler && CharacterHandler.CurrentCharacter)
                    return CharacterHandler.CurrentCharacter;
                return CharacterHandler.CurrentCharacters.Count > 0 ? CharacterHandler.CurrentCharacters[0] : null;
            }

            set
            {
                if (!CharacterHandler)
                    return;
                if (!CharacterHandler.CurrentCharacters.Contains(value))
                    CharacterHandler.Register(value);
                CharacterHandler.CurrentCharacter = value;
            }
        }
        /// <summary>
        /// Gets/Sets the InworldGameData of the InworldController.
        /// </summary>
        public InworldGameData GameData
        {
            get => m_GameData;
            set
            {
                m_GameData = value;
                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                #endif
            }
        }
        
        /// <summary>
        /// Load InworldGameData. Set client's related data if game data is not null.
        /// </summary>
        /// <param name="gameData">the InworldGameData to load</param>
        public void LoadData(InworldGameData gameData)
        {
            if (gameData == null)
            {
                if (string.IsNullOrEmpty(Client.SceneFullName) && !string.IsNullOrEmpty(m_SceneFullName))
                    Client.SceneFullName = m_SceneFullName;
                return;
            }
            if (!string.IsNullOrEmpty(gameData.apiKey))
                m_Client.APIKey = gameData.apiKey;
            if (!string.IsNullOrEmpty(gameData.apiSecret))
                m_Client.APISecret = gameData.apiSecret;
            if (!string.IsNullOrEmpty(gameData.sceneFullName))
                m_Client.SceneFullName = gameData.sceneFullName;
            if (gameData.capabilities != null)
                InworldAI.Capabilities = gameData.capabilities;
        }
#region Client 
#region Connection Management
        /// <summary>
        /// Use the input json string of token instead of API key/secret to load scene.
        /// This token can be fetched by other applications such as InworldWebSDK.
        /// </summary>
        /// <param name="token">the custom token to init.</param>
        public void InitWithCustomToken(string token) => m_Client.InitWithCustomToken(token);
        /// <summary>
        /// Reconnect session or start a new session if the current session is invalid.
        /// </summary>
        public void Reconnect() => m_Client.Reconnect();
        /// <summary>
        /// Initializes the SDK.
        /// </summary>
        public void Init() => m_Client.GetAccessToken();
        /// <summary>
        /// Send LoadScene request to Inworld Server.
        /// In ver 3.3 or further, the session must be connected first.
        /// </summary>
        /// <param name="sceneFullName">the full string of the scene to load.</param>
        public void LoadScene(string sceneFullName = "")
        {
            InworldAI.LogEvent("Login_Runtime");
            string sceneToLoad = string.IsNullOrEmpty(sceneFullName) ? m_SceneFullName : sceneFullName;
            m_Client.LoadScene(sceneToLoad);
        }
        /// <summary>
        /// Sent after the ClientStatus is set to Connected.
        /// Sequentially sends Session Config parameters, enabling this session to become interactive.
        /// </summary>
        public void PrepareSession() => StartCoroutine(Client.PrepareSession());
        /// <summary>
        /// Disconnect Inworld Server.
        /// </summary>
        public void Disconnect()
        {
            m_Client.Disconnect();
        }
#endregion
#region Interaction
        /// <summary>
        /// Send messages to an InworldCharacter in this current scene.
        /// If there's a current character, it'll be sent to the specific character,
        /// otherwise, it'll be sent as broadcast.
        /// </summary>
        /// <param name="text">the message to send.</param>
        public void SendText(string text)
        {
            m_Client.SendTextTo(text, CharacterHandler.CurrentCharacterNames);
        }
        /// <summary>
        /// Send the CancelResponse Event to InworldServer to interrupt the character's speaking.
        /// </summary>
        /// <param name="interactionID">the handle of the dialog context that needs to be cancelled.</param>
        /// <param name="utteranceID">the handle of the current utterance that needs to be cancelled.</param>
        public void SendCancelEvent(string interactionID, string utteranceID = "")
        {
            m_Client.SendCancelEventTo(interactionID, utteranceID, CharacterHandler.CurrentCharacterNames);
        } 
        /// <summary>
        /// Legacy Send the trigger to an InworldCharacter in the current scene.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send. Send to World if it's empty</param>
        /// <param name="triggerName">the name of the trigger to send.</param>
        /// <param name="parameters">the parameters and their values for the triggers.</param>
        public void SendWorldTrigger(string triggerName)
        {
            if (Client.Status != InworldConnectionStatus.Connected)
                m_Client.SendTriggerTo(triggerName);
        }
        /// <summary>
        /// Legacy Send the trigger to an InworldCharacter in the current scene.
        /// </summary>
        /// <param name="charID">the live session ID of the character to send. Send to World if it's empty</param>
        /// <param name="triggerName">the name of the trigger to send.</param>
        /// <param name="parameters">the parameters and their values for the triggers.</param>
        public void SendTrigger(string triggerName, string charID = "", Dictionary<string, string> parameters = null)
        {
            if (Client.Status != InworldConnectionStatus.Connected)
                InworldAI.LogException($"Tried to send trigger to {charID}, but not connected to server.");
            if (string.IsNullOrEmpty(charID))
                m_Client.SendTriggerTo(triggerName, parameters);
            else
                m_Client.SendTrigger(charID, triggerName, parameters);
        }
        /// <summary>
        /// Send AUDIO_SESSION_START control events to server.
        /// Without sending this message, all the audio data would be discarded by server.
        /// However, if you send this event twice in a row, without sending `StopAudio()`, Inworld server will also through exceptions and terminate the session.
        /// </summary>
        /// <exception cref="ArgumentException">If the charID is not legal, this function will throw exception.</exception>
        public virtual void StartAudio()
        {
            if (Client.Status != InworldConnectionStatus.Connected)
                InworldAI.LogException($"Tried to start audio, but not connected to server.");
            if (CharacterHandler.CurrentCharacterNames.Count <= 0)
                InworldAI.LogException($"No characters in the session.");
            Audio.StartAudio();
        }
        /// <summary>
        /// Send AUDIO_SESSION_END control events to server.
        /// </summary>
        public virtual void StopAudio()
        {
            Audio.StopAudio();
        }
        /// <summary>
        /// Send the wav data to the current character.
        /// Need to make sure that AUDIO_SESSION_START control event has been sent to server.
        ///
        /// Only the base64 string of the wave data is supported by Inworld server.
        /// Additionally, the sample rate of the wave data has to be 16000, mono channel.
        /// </summary>
        /// <param name="base64">the base64 string of the wave data to send.</param>
        public virtual void SendAudio(string base64)
        {
            if (!Audio.IsAudioAvailable)
                return;
            if (CurrentCharacter && !string.IsNullOrEmpty(CurrentCharacter.ID))
            {
                m_Client.SendAudio(CurrentCharacter.ID, base64);
            }
            else
                m_Client.SendAudioTo(base64, CharacterHandler.CurrentCharacterNames);
        }
        /// <summary>
        /// Manually push the audio wave data to server.
        /// </summary>
        public void PushAudio()
        {
            if (Client.Status != InworldConnectionStatus.Connected)
                InworldAI.LogException($"Tried to push audio, but not connected to server.");
            m_AudioCapture.PushAudio();
            StopAudio();
        }
#endregion
#endregion

        // protected virtual void ResetAudio()
        // {
        //     if (InworldAI.IsDebugMode)
        //         InworldAI.Log($"Audio Reset");
        //     m_AudioCapture.StopRecording();
        // }
        protected virtual void Awake()
        {
            _Setup();
            DontDestroyOnLoad(gameObject);
        }
        protected virtual void OnEnable()
        {
            _Setup();
        }

        protected virtual void Start() => LoadData(m_GameData);

        protected void _Setup()
        {
            if (!m_Client)
                m_Client = GetComponent<InworldClient>();
            if (!m_AudioCapture)
                m_AudioCapture = GetComponent<AudioCapture>();
            if (!m_CharacterHandler)
                m_CharacterHandler = GetComponent<CharacterHandler>();
        }
    }
}
