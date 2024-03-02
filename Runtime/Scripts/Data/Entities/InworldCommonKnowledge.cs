/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;

namespace Inworld.Entities
{
	[Serializable]
	public class InworldCommonKnowledge
	{
		public string name;
		public string displayName;
		public string description;
	}
	[Serializable]
	public class ListCommonKnowledgeResponse
	{
		public List<InworldCommonKnowledge> commonKnowledge;
		public string nextPageToken;
	}
}
