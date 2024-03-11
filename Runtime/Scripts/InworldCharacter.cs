/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Interactions;
using Inworld.Packet;
using Inworld.Entities;
using Inworld.Sample;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inworld
{
    [RequireComponent(typeof(InworldInteraction))]
    public class InworldCharacter : MonoBehaviour
    {
        [SerializeField] protected InworldCharacterData m_Data;
        [SerializeField] protected CharacterEvents m_CharacterEvents;
        [SerializeField] protected bool m_VerboseLog;

        protected Animator m_Animator;
        protected InworldInteraction m_Interaction;
        protected bool m_IsSpeaking;
        
        RelationState m_CurrentRelation = new RelationState();

        /// <summary>
        /// Gets the Unity Events of the character.
        /// </summary>
        public CharacterEvents Event => m_CharacterEvents;
        /// <summary>
        /// Gets the character's animator.
        /// </summary>
        public Animator Animator
        {
            get
            {
                if (!m_Animator)
                    m_Animator = GetComponent<Animator>();
                return m_Animator;
            }
        }
        /// <summary>
        /// Gets/Sets if this character is speaking.
        /// </summary>
        public bool IsSpeaking
        {
            get => m_IsSpeaking;
            internal set
            {
                if (m_IsSpeaking == value)
                    return;
                m_IsSpeaking = value;
                if (m_IsSpeaking)
                {
                    if (m_VerboseLog)
                        InworldAI.Log($"{Name} Starts Speaking");
                    m_CharacterEvents.onBeginSpeaking.Invoke(BrainName);
                }
                else
                {
                    if (m_VerboseLog)
                        InworldAI.Log($"{Name} Ends Speaking");
                    m_CharacterEvents.onEndSpeaking.Invoke(BrainName);
                }
            }
        }
        /// <summary>
        /// Gets/Sets the charater's current relationship towards players. Will invoke onRelationUpdated when set.
        /// </summary>
        public RelationState CurrRelation
        {
            get => m_CurrentRelation;
            protected set
            {
                if (m_CurrentRelation.IsEqualTo(value))
                    return;
                if (m_VerboseLog)
                    InworldAI.Log($"{Name}: {m_CurrentRelation.GetUpdate(value)}");
                m_CurrentRelation = value;
                m_CharacterEvents.onRelationUpdated.Invoke(BrainName);
            }
        }
        /// <summary>
        /// Gets/Sets the character's data.
        /// If set, it'll also allocate the live session ID to the character's `InworldInteraction` component.
        /// </summary>
        public InworldCharacterData Data 
        {
            get => m_Data;
            set => m_Data = value;
        }
        /// <summary>
        /// Get the display name for the character. Note that name may not be unique.
        /// </summary>
        public string Name => Data?.givenName ?? "";
        /// <summary>
        /// The `BrainName` for the character.
        /// Note that `BrainName` is actually the character's full name, formatted like `workspace/xxx/characters/xxx`.
        /// It is unique.
        /// </summary>
        public string BrainName => Data?.brainName ?? "";
        /// <summary>
        /// Gets the live session ID of the character. If not registered, will try to fetch one from InworldController's CharacterHandler.
        /// </summary>
        public string ID => string.IsNullOrEmpty(Data?.agentId) ? GetLiveSessionID() : Data?.agentId;
        /// <summary>
        ///     Returns the priority of the character.
        ///     the higher the Priority is, the character is more likely responding to player.
        /// </summary>
        public float Priority { get; set; }
        /// <summary>
        /// Register the character in the character list.
        /// Get the live session ID for an Inworld character.
        /// </summary>

        public virtual string GetLiveSessionID()
        {
            if (string.IsNullOrEmpty(BrainName))
                return "";
            if (!InworldController.Client.LiveSessionData.TryGetValue(BrainName, out InworldCharacterData value))
                return "";
            Data = value;
            return Data.agentId;
        }
        /// <summary>
        /// Send the message to this character.
        /// </summary>
        /// <param name="text">the message to send</param>
        public virtual void SendText(string text)
        {
            // 1. Interrupt current speaking.
            CancelResponse();
            // 2. Send Text.
            InworldController.Client.SendTextTo(text, new List<string>{BrainName});
        }
        /// <summary>
        /// Send the trigger to this character.
        /// Trigger is defined in the goals section of the character in Inworld Studio.
        /// </summary>
        /// <param name="trigger">the name of the trigger.</param>
        /// <param name="needCancelResponse">If checked, this sending process will interrupt the character's current speaking.</param>
        /// <param name="parameters">The parameters and values of the trigger.</param>
        public virtual void SendTrigger(string trigger, bool needCancelResponse = true, Dictionary<string, string> parameters = null)
        {
            // 1. Interrupt current speaking.
            if (needCancelResponse)
                CancelResponse();
            // 2. Send Text. YAN: Now all trigger has to be lower cases.
            InworldController.Client.SendTriggerTo(trigger.ToLower(), parameters, new List<string>{BrainName});
        }
        /// <summary>
        /// Enable target goal of this character.
        /// By default, all the goals are already enabled.
        /// </summary>
        /// <param name="goalName">the name of the goal to enable.</param>
        public virtual void EnableGoal(string goalName) => InworldMessenger.EnableGoal(goalName, ID);
        /// <summary>
        /// Disable target goal of this character.
        /// </summary>
        /// <param name="goalName">the name of the goal to disable.</param>
        public virtual void DisableGoal(string goalName) => InworldMessenger.DisableGoal(goalName, ID);
        /// <summary>
        /// Interrupt the current character's speaking.
        /// Ignore all the current incoming messages from the character.
        /// </summary>
        public virtual void CancelResponse() => m_Interaction.CancelResponse();
        protected virtual void Awake()
        {
            if (m_Interaction == null)
                m_Interaction = GetComponent<InworldInteraction>();
        }

        protected virtual void OnEnable()
        {
            InworldController.Client.OnStatusChanged += OnStatusChanged;
        }
        protected virtual void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.CharacterHandler.Unregister(this);
            InworldController.Client.OnStatusChanged -= OnStatusChanged;
        }
        protected virtual void OnDestroy()
        {
            if (!InworldController.Instance)
                return;
            InworldController.CharacterHandler.Unregister(this);
            m_CharacterEvents.onCharacterDestroyed?.Invoke(BrainName);
        }

        protected virtual void OnStatusChanged(InworldConnectionStatus newStatus)
        {
            if (newStatus == InworldConnectionStatus.Idle)
            {
                Data.agentId = "";
            }
        }
        internal virtual void OnInteractionChanged(List<InworldPacket> packets)
        {
            foreach (InworldPacket packet in packets)
            {
                ProcessPacket(packet);
            }
        }

        internal virtual void ProcessPacket(InworldPacket incomingPacket)
        {
            if (!incomingPacket.IsRelated(ID))
                return;
            m_CharacterEvents.onPacketReceived.Invoke(incomingPacket);
            
            switch (incomingPacket)
            {
                case ActionPacket actionPacket:
                    HandleAction(actionPacket);
                    break;
                case AudioPacket audioPacket: // Already Played.
                    HandleLipSync(audioPacket);
                    break;
                case ControlPacket controlPacket: // Interaction_End
                    break;
                case TextPacket textPacket:
                    HandleText(textPacket);
                    break;
                case EmotionPacket emotionPacket:
                    HandleEmotion(emotionPacket);
                    break;
                case CustomPacket customPacket:
                    HandleTrigger(customPacket);
                    break;
                default:
                    Debug.LogError($"Received Unknown {incomingPacket}");
                    break;
            }
        }
        protected virtual void HandleRelation(CustomPacket relationPacket)
        {
            RelationState tmp = new RelationState();
            foreach (TriggerParameter param in relationPacket.custom.parameters)
            {
                tmp.UpdateByTrigger(param);
            }
            CurrRelation = tmp;
        }

        protected virtual void HandleText(TextPacket packet)
        {
            if (packet.text == null || string.IsNullOrWhiteSpace(packet.text.text))
                return;
            
            if (packet.Source == SourceType.PLAYER)
            {
                if (m_VerboseLog)
                    InworldAI.Log($"{InworldAI.User.Name}: {packet.text.text}");
                if (PlayerController.Instance)
                    PlayerController.Instance.onPlayerSpeaks.Invoke(packet.text.text);
            }
            if (packet.Source == SourceType.AGENT && packet.IsSource(ID))
            {
                IsSpeaking = true;
                if (m_VerboseLog)
                    InworldAI.Log($"{Name}: {packet.text.text}");
                Event.onCharacterSpeaks.Invoke(BrainName, packet.text.text);
            }
            else
            {
                IsSpeaking = false;
            }
        }
        protected virtual void HandleEmotion(EmotionPacket packet)
        {
            if (!packet.IsSource(ID) && !packet.IsTarget(ID))
                return;
            if (m_VerboseLog)
                InworldAI.Log($"{Name}: {packet.emotion.behavior} {packet.emotion.strength}");
            m_CharacterEvents.onEmotionChanged.Invoke(BrainName, packet.emotion.ToString());
        }
        protected virtual void HandleTrigger(CustomPacket customPacket)
        {
            if (!customPacket.IsSource(ID) && !customPacket.IsTarget(ID))
                return;
            if (customPacket.Message == InworldMessage.RelationUpdate)
            {
                HandleRelation(customPacket);
                return;
            }
            if (m_VerboseLog)
            {
                string output = $"{Name}: Received Trigger {customPacket.custom.name}";
                output = customPacket.custom.parameters.Aggregate(output, (current, param) => current + $" With {param.name}: {param.value}");
                InworldAI.Log(output);
            }
            m_CharacterEvents.onGoalCompleted.Invoke(BrainName, customPacket.TriggerName);
        }
        protected virtual void HandleAction(ActionPacket actionPacket)
        {
            if (m_VerboseLog && (actionPacket.IsSource(ID) || actionPacket.IsTarget(ID)))
                InworldAI.Log($"{Name} {actionPacket.action.narratedAction.content}");
        }
        protected virtual void HandleLipSync(AudioPacket audioPacket)
        {
            // Won't process lip sync in pure text 2D conversation
        }
    }
}
