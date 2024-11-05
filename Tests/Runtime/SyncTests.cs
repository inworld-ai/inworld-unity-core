/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/


using Inworld.Packet;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;


namespace Inworld.Test.Sync
{
	public class ConnectionTests : InworldRuntimeTest
	{
		[UnityTest]
		public IEnumerator InworldRuntimeTest_ReconnectionTest()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
			InworldController.Client.Disconnect();
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			Assert.IsFalse(m_Conversation.Any(p => p is TextPacket));
			Assert.IsFalse(m_Conversation.Any(p => p is AudioPacket));
			yield return InitTest();
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));		
		}
	}
	public class TextInteractionTests : InworldRuntimeTest
	{
		[UnityTest]
		public IEnumerator InworldRuntimeTest_SendText()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
		}
		[UnityTest]
		public IEnumerator InworldRuntimeTest_SendTextAfterUnknownPacket()
		{
			m_Conversation.Clear();
			OnPacketReceived(new InworldPacket());
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
		}
		[UnityTest]
		public IEnumerator InworldRuntimeTest_SayVerbatim()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "How are you?");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket textPacket && textPacket.text.text == k_VerbatimResponse));
		}
	}

	public class EmotionInteractionTests : InworldRuntimeTest
	{
		[UnityTest]
		public IEnumerator InworldRuntimeTest_EmotionChange()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "You're feeling sad");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is EmotionPacket emoPacket 
			                                      && emoPacket.emotion.behavior == SpaffCode.SADNESS 
			                                      && emoPacket.emotion.strength == Strength.STRONG));
		}
	}

	public class UnitarySessionTests : InworldRuntimeTest
	{
		[UnityTest]
		public IEnumerator InworldRuntimeTest_ChangeScene()
		{
			m_Conversation.Clear();
			InworldController.Client.LoadScene(k_AltScene);
			yield return new WaitWhile(() => k_TestScene == m_CurrentInworldScene);
			Assert.IsTrue(m_CurrentInworldScene == k_AltScene);
		}
		[UnityTest, Order(2)]
		public IEnumerator InworldRuntimeTest_SceneBehavior()
		{
			m_Conversation.Clear();
			InworldController.Client.LoadScene(k_AltScene);
			yield return new WaitWhile(() => k_TestScene == m_CurrentInworldScene);
			Assert.IsTrue(m_CurrentInworldScene == k_AltScene);
		}
	}
	public class AudioInteractionTests : InworldRuntimeTest
	{
		[UnityTest, Order(1)]
		public IEnumerator InworldRuntimeTest_SendAudio()
		{
			m_Conversation.Clear();
			string agentID = InworldController.Client.LiveSessionData.Values.First().agentId;
			InworldController.Client.StartAudio(agentID); ;
			yield return new WaitForSeconds(0.1f);
			InworldController.Audio.AutoDetectPlayerSpeaking = false;
			InworldController.Client.SendAudio(agentID, k_AudioChunkBase64);
			yield return new WaitForSeconds(0.1f);
			InworldController.Client.StopAudio(agentID);
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
		}
		[UnityTest, Order(2)]
		public IEnumerator InworldRuntimeTest_FreqSwitchAudioSession()
		{
			m_Conversation.Clear();
			string agentID = InworldController.Client.LiveSessionData.Values.First().agentId;
			for (int i = 0; i < 10; i++)
			{
				InworldController.Client.StartAudio(agentID); 
				yield return new WaitForSeconds(0.1f);
				InworldController.Audio.AutoDetectPlayerSpeaking = false;
				InworldController.Client.StopAudio(agentID);
				yield return new WaitForSeconds(0.1f);
			}
			InworldController.Client.StartAudio(agentID); 
			yield return new WaitForSeconds(0.1f);
			InworldController.Audio.AutoDetectPlayerSpeaking = false;
			InworldController.Client.SendAudio(agentID, k_AudioChunkBase64);
			yield return new WaitForSeconds(0.1f);
			InworldController.Client.StopAudio(agentID);
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
		}
		[UnityTest, Order(3)]
		public IEnumerator InworldRuntimeTest_InterleaveTextAudio()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Hello");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
			m_Conversation.Clear();
			InworldController.Audio.AutoDetectPlayerSpeaking = false;
			string agentID = InworldController.Client.LiveSessionData.Values.First().agentId;
			InworldController.Client.StartAudio(agentID); ;
			yield return new WaitForSeconds(0.1f);
			InworldController.Client.SendAudio(agentID, k_AudioChunkBase64);
			yield return new WaitForSeconds(0.1f);
			InworldController.Client.StopAudio(agentID);
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket));
			Assert.IsTrue(m_Conversation.Any(p => p is AudioPacket));
		}
	}

	public class TriggerInteractionTests : InworldRuntimeTest
	{
		[UnityTest, Order(1)]
		public IEnumerator InworldRuntimeTest_GoalsTrigger()
		{
			m_Conversation.Clear();
			InworldController.Client.SendTrigger(InworldController.Client.LiveSessionData.Values.First().agentId, "hit_trigger");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is CustomPacket customPacket && customPacket.Trigger == k_TriggerGoal));
			Assert.IsTrue(m_Conversation.Any(p => p is TextPacket textPacket && textPacket.text.text == k_TriggerResponse));
		}
		[UnityTest, Order(2)]
		public IEnumerator InworldRuntimeTest_GoalsRepeatable()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Repeatable");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is CustomPacket customPacket && customPacket.Trigger == k_RepeatableGoal));
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Repeatable");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is CustomPacket customPacket && customPacket.Trigger == k_RepeatableGoal));
		}
		[UnityTest, Order(3)]
		public IEnumerator InworldRuntimeTest_GoalsUnrepeatable()
		{
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Unrepeatable");
			yield return ConversationCheck(10);
			Assert.IsTrue(m_Conversation.Any(p => p is CustomPacket customPacket && customPacket.Trigger == k_UnrepeatableGoal));
			m_Conversation.Clear();
			InworldController.Client.SendText(InworldController.Client.LiveSessionData.Values.First().agentId, "Unrepeatable");
			yield return ConversationCheck(10);
			Assert.IsFalse(m_Conversation.Any(p => p is CustomPacket customPacket && customPacket.Trigger == k_UnrepeatableGoal));
		}
	}
}
