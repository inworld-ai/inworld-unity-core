/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using Inworld.Assets;
using Inworld.Entities;
using TMPro;
using UnityEngine;

using System.Collections.Generic;
using Inworld.Sample;


namespace Inworld.UI
{
    public class ChatBubbleFeedback : InworldUIElement
    {
        
#region Events
        public event Action<string> OnRemoveBubbleEvent;
#endregion
        
        [SerializeField] FeedbackToggleButton m_FBIconDislike;
        [SerializeField] FeedbackToggleButton m_FBIconLike;
        [SerializeField] TMP_Text m_TextField;
        
        string m_CorrelationID;
        string m_InteractionID;
        string m_UtteranceID;
        
        
        Feedback m_Feedback = new Feedback();

        public void OnToggleDislike(bool isOn)
        {
            if (isOn)
            {
                _Submit(false);
            }
        }
        
        public void OnToggleLike(bool isOn)
        {
            if (isOn)
            {
                _Submit(true);
            }
        }


    #region Properties
        /// <summary>
        ///     Get/Set the bubble's main content.
        /// </summary>
        public string Text
        {
            get => m_TextField.text;
            set => m_TextField.text = value;
        }

        /// <summary>
        ///     Set the bubble's property.
        /// </summary>
        /// <param name="charName">The bubble's owner's name</param>
        /// <param name="thumbnail">The bubble's owner's thumbnail</param>
        /// <param name="text">The bubble's content</param>
        public override void SetBubble(string charName, Texture2D thumbnail = null, string text = null)
        {
            base.SetBubble(charName, thumbnail, text);
            if (m_TextField && !string.IsNullOrEmpty(text))
                m_TextField.text = text;
        }
        /// <summary>
        ///     Set the bubble's property.
        /// </summary>
        /// <param name="charName">The bubble's owner's name</param>
        /// <param name="interactionID">The bubble's interaction ID</param>
        /// <param name="correlationID">The bubble's correlation ID</param>
        /// <param name="thumbnail">The bubble's owner's thumbnail</param>
        /// <param name="text">The bubble's content</param>
        public override void SetBubbleWithPacketInfo(string charName, string interactionID, string correlationID, string utteranceID, Texture2D thumbnail = null, string text = null)
        {
            base.SetBubbleWithPacketInfo(charName, interactionID, correlationID, utteranceID, thumbnail, text);
            m_CorrelationID = correlationID;
            m_InteractionID = interactionID;
            m_UtteranceID = utteranceID;
            if (m_TextField && !string.IsNullOrEmpty(text))
                m_TextField.text = text;
        }
        /// <summary>
        /// Attach text to the current bubble.
        /// </summary>
        /// <param name="text"></param>
        public override void AttachBubble(string text) => m_TextField.text = $"{m_TextField.text.Trim()} {text.Trim()}";

    #endregion  
    
        void _CheckInit()
        {
            if (m_Feedback == null)
                m_Feedback = new Feedback();
            if (m_Feedback.type == null)
                m_Feedback.type = new List<string>();
        }
        
        void _Submit(bool isLike)
        {

            _CheckInit();
            
            m_Feedback.isLike = isLike;
            
            InworldController.Client.SendFeedbackAsync(m_InteractionID, m_CorrelationID, m_Feedback);
            
            if (!isLike)
            {
                // Debug.Log("ChatBubbleFeedback: Submit " + isLike + " " + m_InteractionID + " " + m_CorrelationID);
                m_FBIconDislike.Reset();
                OnRemoveBubbleEvent?.Invoke(m_InteractionID);
                m_TextField.text = "";
                InworldController.CharacterHandler.CurrentCharacter.CancelResponse();
                // TODO(Yan): Replace bubble to contain more info.
                InworldController.Client.SendRegenerateEvent(InworldController.CharacterHandler.CurrentCharacter.ID, m_InteractionID); 
                // Remove the bubble(s) associated with the conversation from the chat
                PlayerController.Instance.RemoveBubble(m_InteractionID, m_UtteranceID);
            }
        }
    }
}
