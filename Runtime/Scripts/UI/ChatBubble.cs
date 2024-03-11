/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Inworld.Entities;
using Inworld.Packet;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Inworld.UI
{
    /// <summary>
    ///     This class is for each detailed chat bubble.
    /// </summary>
    public class ChatBubble : InworldUIElement, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField] TMP_Text m_TextField;
        string m_InteractionID;
        string m_CorrelationID;
        
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
        public override void SetBubbleWithPacketInfo(string charName, string interactionID, string correlationID, Texture2D thumbnail = null, string text = null)
        {
            base.SetBubbleWithPacketInfo(charName, interactionID, correlationID, thumbnail, text);
            m_InteractionID = interactionID;
            m_CorrelationID = correlationID;
            if (m_TextField && !string.IsNullOrEmpty(text))
                m_TextField.text = text;
        }
        /// <summary>
        /// Attach text to the current bubble.
        /// </summary>
        /// <param name="text"></param>
        public override void AttachBubble(string text) => m_TextField.text = $"{m_TextField.text.Trim()} {text.Trim()}";
        public void OnPointerUp(PointerEventData eventData)
        {
            CreateFeedbackDlg();
        }
        void CreateFeedbackDlg()
        {
            // TODO(Yan): Create UI dialog to post feedback.
        }
        public void MockSendFeedback()
        {
            if (m_Title.text == InworldAI.User.Name) // Send by player.
                return;
            var data = InworldController.Client.LiveSessionData.FirstOrDefault(c => c.Value.givenName == m_Title.text);
            if (data.Value == null || data.Value.givenName != m_Title.text)
                return;
            Feedback feedback = new Feedback(true, m_InteractionID, m_CorrelationID, "good");
            InworldController.Client.SendFeedbackAsync(data.Value.agentId, feedback);
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            // To make PointerUp working, PointerDown is required.
        }
    #endregion

    }

}

