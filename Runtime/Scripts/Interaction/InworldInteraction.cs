/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using UnityEngine;
using Inworld.Packet;
using Inworld.Sample;
using System.Collections;

using Random = UnityEngine.Random;

namespace Inworld.Interactions
{
    public class InworldInteraction : MonoBehaviour
    {
        [SerializeField] KeyCode m_ContinueKey = KeyCode.Space;
        [SerializeField] KeyCode m_SkipKey = KeyCode.LeftControl;
        [SerializeField] GameObject m_ContinueButton;
        [SerializeField] protected bool m_Interruptable = true;
        [SerializeField] protected bool m_AutoProceed = true;
        [SerializeField] protected int m_MaxItemCount = 100;
        [SerializeField] protected float m_TextSpeedMultipler = 0.02f;
        protected InworldCharacter m_Character;
        protected Interaction m_CurrentInteraction;
        protected IEnumerator m_CurrentCoroutine;
        protected readonly IndexQueue<Interaction> m_Prepared = new IndexQueue<Interaction>();
        protected readonly IndexQueue<Interaction> m_Processed = new IndexQueue<Interaction>();
        protected readonly IndexQueue<Interaction> m_Cancelled = new IndexQueue<Interaction>();

        protected bool m_Proceed = true;
        protected float m_AnimFactor;
        protected bool m_IsContinueKeyPressed;
        protected bool m_LastFromPlayer;
        /// <summary>
        /// Gets the factor for selecting animation clips.
        /// If without Audio, it's a random value between 0 and 1.
        /// </summary>
        public virtual float AnimFactor
        {
            get => Random.Range(0, 1);
            set => m_AnimFactor = value;
        }

        /// <summary>
        /// If the target packet is sent or received by this character.
        /// </summary>
        /// <param name="packet">the target packet.</param>
        public bool IsRelated(InworldPacket packet) => packet.IsRelated(m_Character.ID);

        /// <summary>
        /// Interrupt this character by cancelling its incoming sentences.
        /// Hard cancelling means even cancel and interrupt the current interaction.
        /// Soft cancelling will only cancel the stored interactions.
        /// </summary>
        /// <param name="isHardCancelling">If it's hard cancelling. By default it's true.</param>
        public virtual void CancelResponse(bool isHardCancelling = true)
        {
            if (string.IsNullOrEmpty(m_Character.ID) || !m_Interruptable)
                return;
            if (isHardCancelling && m_CurrentInteraction != null)
            {
                InworldController.Client.SendCancelEvent(m_Character.ID, m_CurrentInteraction.ID, m_CurrentInteraction.CurrentUtterance?.ID);
                m_CurrentInteraction.Cancel();
            }
            m_Prepared.PourTo(m_Cancelled);
            m_CurrentInteraction = null;
        }
        protected virtual void Awake()
        {
            if (!m_Character)
                m_Character = GetComponent<InworldCharacter>();
            if (!m_Character)
                enabled = false;
        }
        protected virtual void OnEnable()
        {
            InworldController.Client.OnPacketReceived += ReceivePacket;
            m_CurrentCoroutine = InteractionCoroutine();
            StartCoroutine(m_CurrentCoroutine);
        }

        protected virtual void OnDisable()
        {
            StopCoroutine(m_CurrentCoroutine);
            if (InworldController.Instance)
                InworldController.Client.OnPacketReceived -= ReceivePacket;
        }
        void Update()
        {
            if (PlayerController.Instance)
                AlignPlayerInput();
            if (Input.GetKeyUp(m_SkipKey))
                SkipCurrentUtterance();
            if (Input.GetKeyDown(m_ContinueKey))
                UnpauseUtterance();
            if (Input.GetKeyUp(m_ContinueKey))
                PauseUtterance();
            m_Proceed = m_AutoProceed || m_LastFromPlayer || m_IsContinueKeyPressed || m_CurrentInteraction == null || m_CurrentInteraction.IsEmpty;
        }
        protected virtual void UnpauseUtterance()
        {
            m_IsContinueKeyPressed = true;
        }
        protected virtual void PauseUtterance()
        {
            m_IsContinueKeyPressed = false;
        }
        protected virtual void AlignPlayerInput()
        {
            if (!PlayerController.Instance)
                return;
            if (PlayerController.Instance.continueKey != KeyCode.None)
            {
                m_AutoProceed = false;
                m_ContinueKey = PlayerController.Instance.continueKey;
            }
            if (PlayerController.Instance.skipKey != KeyCode.None)
            {
                m_Interruptable = true;
                m_SkipKey = PlayerController.Instance.skipKey;
            }
        }
        protected virtual void SkipCurrentUtterance() 
        {
            if (m_CurrentInteraction?.CurrentUtterance != null)
                m_CurrentInteraction.CurrentUtterance = null;
        }
        protected virtual IEnumerator InteractionCoroutine()
        {
            while (true)
            {
                yield return RemoveExceedItems();
                yield return HandleNextUtterance();
            }
        }
        protected IEnumerator HandleNextUtterance()
        {
            if (m_Proceed)
            {
                HideContinue();
                if (m_CurrentInteraction == null)
                {
                    m_CurrentInteraction = GetNextInteraction();
                }
                if (m_CurrentInteraction != null && m_CurrentInteraction.CurrentUtterance == null)
                {
                    m_CurrentInteraction.CurrentUtterance = GetNextUtterance();
                }
                if (m_CurrentInteraction != null && m_CurrentInteraction.CurrentUtterance != null)
                {
                    if (InworldController.Audio.SampleMode != MicSampleMode.TURN_BASED || !InworldController.Audio.CurrentPlayingPlayer || !InworldController.Audio.CurrentPlayingPlayer.isPlaying)
                        yield return PlayNextUtterance();
                }
                else if (m_Character)
                    m_Character.IsSpeaking = false;
            }
            else
            {
                ShowContinue();
                yield return null;
            }
        }
        void HideContinue()
        {
            if (m_ContinueButton)
                m_ContinueButton.SetActive(false);
        }
        void ShowContinue()
        {
            if (m_ContinueButton)
                m_ContinueButton.SetActive(true);
        }
        void ReceivePacket(InworldPacket incomingPacket)
        {
            if (!IsRelated(incomingPacket))
                return;
            if (incomingPacket is CustomPacket || incomingPacket is EmotionPacket)
                m_Character.ProcessPacket(incomingPacket);
            if (incomingPacket.Source == SourceType.PLAYER && (incomingPacket.IsBroadCast || incomingPacket.IsTarget(m_Character.ID)))
            {
                if (!(incomingPacket is AudioPacket))
                    m_LastFromPlayer = true;
                m_Character.ProcessPacket(incomingPacket);
            }
            if (incomingPacket.Source == SourceType.AGENT && (incomingPacket.IsSource(m_Character.ID) || incomingPacket.IsTarget(m_Character.ID)))
            {
                if (incomingPacket is AudioPacket && !incomingPacket.IsSource(m_Character.ID)) //Audio chunk only dispatch once. to the source.
                    return;
                m_LastFromPlayer = false;
                HandleAgentPackets(incomingPacket);
            }
        }
        protected void HandleAgentPackets(InworldPacket packet)
        {
            if (m_Processed.IsOverDue(packet))
                m_Processed.Add(packet);
            else if (m_Cancelled.IsOverDue(packet))
                m_Cancelled.Add(packet);
            else if (m_CurrentInteraction != null && m_CurrentInteraction.Contains(packet))
                m_CurrentInteraction.Add(packet);
            else
                m_Prepared.Add(packet);
        }
        protected IEnumerator RemoveExceedItems()
        {
            m_Cancelled.Clear();
            if (m_Processed.Count > m_MaxItemCount)
                m_Processed.Dequeue();
            yield break;
        }
        protected Interaction GetNextInteraction()
        {
            if (m_CurrentInteraction != null)
                return null;
            m_CurrentInteraction = m_Prepared.Dequeue(true);
            return m_CurrentInteraction;
        }
        protected Utterance GetNextUtterance()
        {
            if (m_CurrentInteraction.CurrentUtterance != null)
                return null;
            // YAN: At the moment of Dequeuing, the utterance is already in processed.
            m_CurrentInteraction.CurrentUtterance = m_CurrentInteraction.Dequeue();
            if (m_CurrentInteraction.CurrentUtterance != null)
                return m_CurrentInteraction.CurrentUtterance;
            // YAN: Else set the current interaction to null to get next dequeue interaction.
            m_Processed.Enqueue(m_CurrentInteraction);
            m_CurrentInteraction = null; 
            return null;
        }
        protected virtual IEnumerator PlayNextUtterance()
        {
            m_Character.OnInteractionChanged(m_CurrentInteraction.CurrentUtterance.Packets);
            yield return new WaitForSeconds(m_CurrentInteraction.CurrentUtterance.GetTextSpeed() * m_TextSpeedMultipler);
            if (m_CurrentInteraction != null)
                m_CurrentInteraction.CurrentUtterance = null; // YAN: Processed.
        }
    }
}
