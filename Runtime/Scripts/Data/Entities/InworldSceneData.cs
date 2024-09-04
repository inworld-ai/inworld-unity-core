/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

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
        public string timePeriod;
        public List<SceneTrigger> sceneTriggers;
        public List<string> commonKnowledges;
        public List<CharacterReference> characterReferences;
        public SceneAssets defaultSceneAssets;
        public List<SceneCharacterReference> characters;
        
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
    public class ListSceneResponse
    {
        public List<InworldSceneData> scenes;
    }
    [Serializable]
    public class ListCharacterResponse
    {
        public List<CharacterOverLoad> characters;
    }
    [Serializable]
    public class SceneTrigger
    {
        public string trigger;
        public string description;
    }
    [Serializable]
    public class SceneCharacterReference
    {
        public string character;
        public string displayTitle;
        public string imageUri;
        public string additionalAgentInfo;
    }
    [Serializable]
    public class SceneAssets
    {
        public string sceneImg;
        public string sceneImgOriginal;
    }
}
