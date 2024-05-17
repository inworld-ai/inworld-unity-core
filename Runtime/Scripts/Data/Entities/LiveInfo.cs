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
	public class Conversation
	{
		string m_ConversationID;
		public string ID
		{
			get
			{
				if (string.IsNullOrEmpty(m_ConversationID))
					m_ConversationID = InworldController.CharacterHandler.ConversationID;
				return m_ConversationID;
			}
			set => m_ConversationID = value;
		}
		public ConversationEventType Status { get; set; } = ConversationEventType.EVICTED;
		public List<string> BrainNames { get; set; } = new List<string>();
	}
	public class AudioSession
	{
		public string ID { get; set; }
		public bool IsConversation { get; set; }
		public string Target { get; set; } // Either character's brain Name or ConversationID.
		public bool IsSameSession(string brainName)
		{
			if (string.IsNullOrEmpty(brainName))
				return IsConversation;
			return !IsConversation && brainName == Target;
		}
		public bool HasStarted => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(Target);
	}
	/// <summary>
	/// This class is used for caching the current conversation and audio session.
	/// </summary>
	public class LiveInfo
	{
		bool m_IsConversation;
		public InworldCharacterData Character { get; set; } = new InworldCharacterData();
		public Conversation Conversation { get; set; } = new Conversation();
		public AudioSession AudioSession { get; set; } = new AudioSession();
		
		public bool UpdateLiveInfo(string brainName)
		{
			return string.IsNullOrEmpty(brainName) ? UpdateMultiTargets() : UpdateSingleTarget(brainName);
		}
		// ReSharper disable Unity.PerformanceAnalysis
		// As InworldController.CharacterHandler would return directly in most cases.
		public bool UpdateMultiTargets(string conversationID = "", List<string> brainNames = null)
		{
			Character = null;
			IsConversation = true;
			if (string.IsNullOrEmpty(conversationID))
				conversationID = InworldController.CharacterHandler.ConversationID;
			Conversation.ID = conversationID;
			if (brainNames == null || brainNames.Count == 0)
				brainNames = InworldController.CharacterHandler.CurrentCharacterNames;
			Conversation.BrainNames = brainNames;
			return Conversation.BrainNames?.Count > 0;
		}
		// ReSharper disable Unity.PerformanceAnalysis
		// As InworldController.CharacterHandler would return directly in most cases.
		public bool UpdateSingleTarget(string brainName)
		{
			if (string.IsNullOrEmpty(brainName))
				return false;
			IsConversation = false;
			if (brainName == SourceType.WORLD.ToString())
				return true;
			if (brainName != Character?.brainName)
				Character = InworldController.CharacterHandler.GetCharacterByBrainName(brainName)?.Data;
			return Character != null;
		}
		public bool IsConversation
		{
			get => m_IsConversation;
			set
			{
				m_IsConversation = value;
				AudioSession.IsConversation = value;
			}
		}
		public void StartAudioSession(string packetID)
		{
			AudioSession.ID = packetID;
			AudioSession.Target = IsConversation ? InworldController.CharacterHandler.ConversationID : Character.brainName;
		}
		public void StopAudioSession()
		{
			AudioSession.ID = "";
			AudioSession.Target = "";
		}
	}
}
