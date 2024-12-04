/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Inworld.Packet;
using TMPro;
using UnityEngine;


namespace Inworld.UI
{
    /// <summary>
    ///     This class is for each detailed chat bubble.
    /// </summary>
    public class ChatBubble : InworldUIElement
    {
        [SerializeField] protected TMP_Text m_TextField;
        protected List<KeyValuePair<string, string>> m_Utterances = new();
        protected string m_InteractionID;
        protected string m_CorrelationID;
        
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
        /// Attach text to the current bubble.
        /// </summary>
        /// <param name="text"></param>
        public override void AttachBubble(string text) => m_TextField.text = $"{m_TextField.text.Trim()} {text.Trim()}";

        /// <summary>
        /// Attach text to the current bubble.
        /// </summary>
        /// <param name="packetID"></param>>
        /// <param name="text"></param>
        public override void UpdateBubbleWithPacketInfo(PacketId packetID, string text)
        {
            m_InteractionID = packetID.interactionId;
            m_CorrelationID = packetID.correlationId;
            m_Utterances.Add(new KeyValuePair<string, string>(packetID.utteranceId, text.Trim()));
            if (!m_TextField)
                return;
            m_TextField.text = m_Utterances.Aggregate("", (current, kvp) => current + $"{kvp.Value} ");
        }

    #endregion

    }

}

