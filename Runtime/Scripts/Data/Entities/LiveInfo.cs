/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;


namespace Inworld.Entities
{
	/// <summary>
	/// This class is used for caching the current conversation and audio session.
	/// </summary>
	public class LiveInfo
	{
		const string k_Conversation = "CONVERSATION";
		public string ConversationID { get; set; }
		/// <summary>
		/// Should be either Current Agent ID or Current Conversation ID.
		/// </summary>
		public string AudioSessionID { get; set; }
		public string CurrentAgentID { get; set; }
		public string CurrentBrainName { get; set; } = k_Conversation;
		public bool IsConversation => !string.IsNullOrEmpty(ConversationID);

		public bool IsSameAudioSession(string brainName)
		{
			if (string.IsNullOrEmpty(brainName))
				return IsConversation && AudioSessionID == ConversationID;
			return CurrentBrainName == brainName;
		}

		public LiveInfo()
		{
			
		}
		public LiveInfo(string characterID, string agentID = "")
		{
			CurrentBrainName = characterID;
			CurrentAgentID = agentID;
		}
	}
}
