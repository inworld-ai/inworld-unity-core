/*************************************************************************************************
* Copyright 2022 Theai, Inc. (DBA Inworld)
*
* Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
* that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
*************************************************************************************************/
using Inworld.Entities;
using Inworld.Sample;
using System.Collections.Generic;
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

    #endregion
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (m_Title.text == InworldAI.User.Name) // Send by player.
                return;
            CreateFeedbackDlg( m_InteractionID, m_CorrelationID);
        }
        void CreateFeedbackDlg(string interactionID, string correlationID)
        {
            PlayerController.Instance.OpenFeedback(interactionID, correlationID);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // To make PointerUp working, PointerDown is required.
        }
    }

}

