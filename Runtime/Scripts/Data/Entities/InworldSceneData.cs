﻿/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Inworld.Entities
{
    [Serializable]
    public class InworldSceneData
    {
        public string name; // Full name
        public string displayName;
        public string description;
        public List<CharacterReference> characterReferences;
        
        [JsonIgnore]
        public float Progress => characterReferences.Count == 0 ? 1 : characterReferences.Sum(cr => cr.Progress) / characterReferences.Count;

        /// <summary>
        /// Returns true if all the characters are inside this scene.
        /// </summary>
        /// <param name="characters">the brainName of all the characters.</param>
        /// <returns></returns>
        public bool Contains(List<string> characters) => characters.All(c => characterReferences.Any(cr => cr.character == c));
    }
    [Serializable] 
    public class CharacterName
    {
        public string name;
        public string languageCode;

        public CharacterName(string inputName, string language = "en-US")
        {
            name = inputName;
            languageCode = language;
        }
    }
    [Serializable]
    public class ListSceneResponse
    {
        public List<InworldSceneData> scenes;
        public string nextPageToken;
    }
    [Obsolete]
    public class LoadScenePacket : InworldPacket
    {
        public LoadSceneEvent mutation;

        public LoadScenePacket(string sceneFullName)
        {
            timestamp = InworldDateTime.UtcNow;
            type = PacketType.MUTATION;
            packetId = new PacketId();
            routing = new Routing("WORLD");
            mutation = new LoadSceneEvent
            {
                loadScene = new LoadSceneRequest
                {
                    name = sceneFullName
                }
            };
        }
    }
    [Obsolete]
    public class LoadCharactersPacket : InworldPacket
    {
        public LoadCharactersEvent mutation;

        public LoadCharactersPacket(List<string> characterFullName)
        {
            timestamp = InworldDateTime.UtcNow;
            type = PacketType.MUTATION;
            packetId = new PacketId();
            routing = new Routing();
            mutation = new LoadCharactersEvent
            {
                loadCharacters = new LoadCharactersRequest(characterFullName)
            };
        }
    }
    [Serializable]
    public class LoadSceneResponse
    {
        public List<InworldCharacterData> agents = new List<InworldCharacterData>();

        public List<string> UpdateRegisteredCharacter(ref List<InworldCharacterData> outData)
        {
            List<string> result = new List<string>();
            foreach (InworldCharacterData charData in outData)
            {
                string registeredID = agents.FirstOrDefault(a => a.brainName == charData.brainName)?.agentId;
                if (string.IsNullOrEmpty(registeredID))
                    result.Add(charData.givenName);
                charData.agentId = registeredID;
            }
            return result;
        }
        [JsonIgnore]
        public bool IsValid => agents?.Count > 0;
    }
}
