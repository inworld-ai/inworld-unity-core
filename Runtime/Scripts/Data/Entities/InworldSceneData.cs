/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inworld.Entities
{
    [Serializable]
    public class InworldSceneData
    {
        public string name; // Full name
        public string displayName;
        public string description;
        public List<CharacterReference> characterReferences;
        public float Progress => characterReferences.Count == 0 ? 1 : characterReferences.Sum(cr => cr.Progress) / characterReferences.Count;
    }
    
    [Serializable]
    public class ListSceneResponse
    {
        public List<InworldSceneData> scenes;
        public string nextPageToken;
    }
    
    [Serializable]
    public class LoadSceneRequest
    {
        public string name;
    }
    [Serializable]
    public class LoadSceneEvent
    {
        public LoadSceneRequest loadScene;
    }
    [Serializable]
    public class LoadScenePacket : InworldPacket
    {
        public LoadSceneEvent mutation;

        public LoadScenePacket(string sceneFullName)
        {
            timestamp = InworldDateTime.UtcNow;
            type = "MUTATION";
            packetId = new PacketId();
            routing = new Routing();
            mutation = new LoadSceneEvent
            {
                loadScene = new LoadSceneRequest
                {
                    name = sceneFullName
                }
            };
        }
        public override string ToJson => JsonUtility.ToJson(this);
    }

    [Serializable]
    public class LoadSceneResponse
    {
        public List<InworldCharacterData> agents = new List<InworldCharacterData>();

        public List<string> UpdateRegisteredCharacter(ref List<InworldCharacterData> outData)
        {
            List<string> result = new List<string>();
            foreach (var charData in outData)
            {
                string registeredID = agents.FirstOrDefault(a => a.brainName == charData.brainName)?.agentId;
                if (string.IsNullOrEmpty(registeredID))
                    result.Add(charData.givenName);
                charData.agentId = registeredID;
            }
            return result;
        }
    }
}
