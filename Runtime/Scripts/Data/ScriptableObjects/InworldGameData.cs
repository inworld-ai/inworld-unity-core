/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Inworld
{
    public class InworldGameData : ScriptableObject
    {
        public string workspaceFullName;
        public string sceneFullName;
        public string apiKey;
        public string apiSecret;
        public List<InworldCharacterData> characters;
        public Capabilities capabilities;
    
        /// <summary>
        /// Get the generated name for the scriptable object.
        /// </summary>
        public string GameDataFileName
        {
            get
            {
                if (string.IsNullOrEmpty(sceneFullName))
                    return workspaceFullName.Split('/')[^1];
                string[] splits = sceneFullName.Split('/');
                return splits.Length >= 4 ? $"{splits[3]}_{splits[1]}" : workspaceFullName.Split('/')[^1];
            }
        }

        public float Progress => characters?.Count > 0 ? characters.Sum(character => character.characterAssets.Progress) / characters.Count : 1;
    
        /// <summary>
        /// Set the data for the scriptable object instantiated.
        /// </summary>
        /// <param name="wsFullName">The InworldWorkspace to load</param>
        /// <param name="sceneData">The InworldSceneData to load</param>
        /// <param name="keySecret">The API key secret to use</param>
        public void SetData(string wsFullName, InworldSceneData sceneData, InworldKeySecret keySecret)
        {
            if (!string.IsNullOrEmpty(wsFullName))
            {
                workspaceFullName = wsFullName;
            }
            if (sceneData != null)
            {
                sceneFullName = sceneData.name;
                if (characters == null)
                    characters = new List<InworldCharacterData>();
                characters.Clear();
                foreach (CharacterReference charRef in sceneData.characterReferences)
                {
                    characters.Add(new InworldCharacterData(charRef));
                }
            }
            else
            {
                if (characters == null)
                    characters = new List<InworldCharacterData>();
                characters.Clear();
                characters.AddRange(InworldAI.User.ListCharacters(wsFullName));
            }
            if (keySecret != null)
            {
                apiKey = keySecret.key;
                apiSecret = keySecret.secret;
            }
            capabilities = new Capabilities(InworldAI.Capabilities);
    #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
    #endif
        }
    }
}

