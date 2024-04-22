/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;

namespace Inworld.Entities
{
	/// <summary>
	/// This class is used for caching the current conversation and audio session.
	/// </summary>
	public class LiveInfo
	{
		string m_CurrentBrainName; 
		public string ConversationID { get; set; }
		/// <summary>
		/// Should be either the conversationID or the character's agent ID.
		/// </summary>
		public string AudioSessionID { get; set; }
		public string CurrentAgentID { get; set; }

		public string CurrentBrainName
		{
			get => m_CurrentBrainName;
			set
			{
				m_CurrentBrainName = value;
				if (string.IsNullOrEmpty(m_CurrentBrainName))
					CurrentAgentID = "";
			}
		}
		// Current Character will overwrite the conversation.
		public bool IsConversation => string.IsNullOrEmpty(CurrentBrainName) && !string.IsNullOrEmpty(ConversationID); 

		public LiveInfo()
		{
			
		}
		public LiveInfo(string characterID, string agentID = "")
		{
			CurrentBrainName = characterID;
			CurrentAgentID = agentID;
		}
		public bool IsValid => !string.IsNullOrEmpty(AudioSessionID) && (AudioSessionID == ConversationID || AudioSessionID == CurrentAgentID);
		
		public bool IsSameAudioSession(string brainName = "")
		{
			if (string.IsNullOrEmpty(brainName))
				return IsConversation;
			return CurrentBrainName == brainName;
		}

		public void OnPacketSent(ControlPacket packet)
		{
			switch (packet.Action)
			{
				case ControlType.AUDIO_SESSION_START:
					string agentID = packet.TargetAgentID;
					AudioSessionID = agentID;
					if (!IsConversation)
						CurrentAgentID = agentID;
					break;
				case ControlType.AUDIO_SESSION_END:
					AudioSessionID = "";
					break;
			}
		}
		public void SetAudioCharacter(string charBrainName, string agentID = "")
		{
			CurrentBrainName = charBrainName;
			CurrentAgentID = agentID;
		}
	}
}
