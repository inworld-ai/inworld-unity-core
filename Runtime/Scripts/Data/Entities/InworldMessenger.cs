/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Packet;
using System.Collections.Generic;
using System.Linq;
namespace Inworld.Entities
{
	/// <summary>
	/// This class is used to send/receive special interactive functions to/from Inworld server,
	/// via CustomPackets.
	/// </summary>
	public static class InworldMessenger
	{
		const string k_GoalEnable = "inworld.goal.enable";
		const string k_GoalDisable = "inworld.goal.disable";
		const string k_GoalComplete = "inworld.goal.complete";
		const string k_RelationUpdate = "inworld.relation.update";
		const string k_ConversationNextTurn = "inworld.conversation.next_turn";
		const string k_Error = "inworld.debug.error";
		const string k_Critical = "inworld.debug.critical-error";
		const string k_GoAway = "inworld.debug.goaway";
		const string k_IncompleteInteraction = "inworld.debug.setup-incomplete-interaction";

		static readonly Dictionary<string, InworldMessage> s_Message;

		static InworldMessenger()
		{
			s_Message = new Dictionary<string, InworldMessage>
			{
				[k_GoalEnable] = InworldMessage.GoalEnable,
				[k_GoalDisable] = InworldMessage.GoalDisable,
				[k_GoalComplete] = InworldMessage.GoalComplete,
				[k_RelationUpdate] = InworldMessage.RelationUpdate,
				[k_ConversationNextTurn] = InworldMessage.ConversationNextTurn,
				[k_Error] = InworldMessage.Error,
				[k_Critical] = InworldMessage.Critical,
				[k_GoAway] = InworldMessage.GoAway,
				[k_IncompleteInteraction] = InworldMessage.IncompleteInteraction
			};
		}
		public static string NextTurn => k_ConversationNextTurn;
		public static int GoalCompleteHead => k_GoalComplete.Length + 1; // YAN: With a dot in the end.
		public static InworldMessage ProcessPacket(CustomPacket packet) => 
			(from data in s_Message where packet.custom.name.StartsWith(data.Key) select data.Value).FirstOrDefault();

		public static void EnableGoal(string goalName, string brainName) => InworldController.Client.SendTriggerTo($"{k_GoalEnable}.{goalName}", null, new List<string>{brainName});
		
		public static void DisableGoal(string goalName, string brainName) => InworldController.Client.SendTriggerTo($"{k_GoalDisable}.{goalName}", null, new List<string>{brainName});

		public static void DebugSendError() => InworldController.Instance.SendTrigger(k_Error);
		
		public static void DebugSendCritical() => InworldController.Instance.SendTrigger(k_Critical);
		
		public static void DebugSendGoAway() => InworldController.Instance.SendTrigger(k_GoAway);
		
		public static void DebugSendIncompleteInteraction() => InworldController.Instance.SendTrigger(k_IncompleteInteraction);
	}
}
