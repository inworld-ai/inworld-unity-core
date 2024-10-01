/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/


using System;
using UnityEngine;
namespace Inworld.Entities
{
    public class ClientInfo
    {
        public string id;
        public string version;
        public string description;

        public override string ToString() => $"{id}: {version} {description}";
    }
    public class ReleaseData
    {
        public PackageData[] package;
    }
    public class PackageData
    {
        public string published_at;
        public string tag_name;
    }
    [Serializable]
    public class AdditionalClientCfg
    {
        [SerializeField] protected string m_CustomToken;
        [SerializeField] protected string m_PublicWorkspace;
        [SerializeField] protected string m_GameSessionID;

        /// <summary>
        /// Get/Set the custom token.
        /// For safety concern, we recommend you set up a server to dispatch the token.
        /// </summary>
        public string CustomToken
        {
            get => m_CustomToken;
            set => m_CustomToken = value;
        }
        /// <summary>
        /// Get/Set the public workspace.
        /// </summary>
        public string PublicWorkspace
        {
            get => m_PublicWorkspace;
            set => m_PublicWorkspace = value;
        }
        /// <summary>
        /// Get/Set the game session ID.
        /// If you'd like to have a better conversation history, please use the same game session ID.
        /// </summary>
        public string GameSessionID
        {
            get => m_GameSessionID;
            set => m_GameSessionID = value;
        }
    }
}
