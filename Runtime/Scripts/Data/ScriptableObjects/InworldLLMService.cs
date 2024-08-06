/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities.LLM;
using UnityEngine;
namespace Inworld
{
	public class InworldLLMService : ScriptableObject
	{
		public string userID;
		public string modelName;
		public TextGenerationConfig config;
	}
}
