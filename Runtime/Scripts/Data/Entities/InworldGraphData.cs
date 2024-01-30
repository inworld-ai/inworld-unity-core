/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Inworld.Entities
{
	[Serializable]
	public class InworldGraphData
	{
		public string name; //Graph's full name
		public string data;
		public string displayName;
		public List<InworldNode> nodes;
		public List<InworldEdge> connections;
	}
	[Serializable]
	public class InworldNode
	{
		public string name; // Node's full name
		public string scene; // Scene full name.
		public List<InworldNodeQuote> quotes;

		public string NodeName => InworldAI.User.GetSceneByFullName(scene)?.displayName; 
	}
	
	[Serializable]
	public class InworldEdge
	{
		public string name; // Edge (Node Connection)'s full name.
		public string nodeFrom; //From scene's full name.
		public string nodeTo; //To scene's full name.
		public string text; 
	}
	[Serializable]
	public class InworldNodeQuote
	{
		public string character;
		public string text;
	}
	[Serializable]
	public class ListGraphResponse
	{
		public List<InworldGraphData> graphs;
		public string nextPageToken;
	}
}
