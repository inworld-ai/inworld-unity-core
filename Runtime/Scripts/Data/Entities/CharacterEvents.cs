/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Packet;
using System;
using UnityEngine.Events;

namespace Inworld.Entities
{
	[Serializable]
	public class CharacterEvents
	{
		public UnityEvent onCharacterRegistered;
		public UnityEvent onBeginSpeaking;
		public UnityEvent onEndSpeaking;
		public UnityEvent<InworldPacket> onPacketReceived;
		public UnityEvent<string ,string> onCharacterSpeaks;
		public UnityEvent<string, string> onEmotionChanged;
		public UnityEvent<string> onGoalCompleted;
		public UnityEvent onRelationUpdated;
		public UnityEvent onCharacterDestroyed;
	}
}
