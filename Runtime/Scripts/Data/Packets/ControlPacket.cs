/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Inworld.Packet
{
    [Serializable]
    public class AudioSessionPayload
    {
        public string mode;
    }
    [Serializable]
    public class ControlEvent
    {
        public string action;
        public string description;
    }
    [Serializable]
    public class AudioControlEvent : ControlEvent
    {
        public AudioSessionPayload audioSessionStart;
    }
    [Serializable]
    public class ConversationControlEvent : ControlEvent
    {
        public ConversationUpdatePayload conversationUpdate;
    }
    [Serializable]
    public class ConversationUpdatePayload
    {
        public List<Source> participants;
    }
    [Serializable]
    public class ControlPacket : InworldPacket
    {
        string m_ControlJson;
        ControlEvent m_Control;
        public ControlPacket()
        {
            type = "CONTROL";
            m_Control = new ControlEvent();
        }
        public ControlPacket(InworldPacket rhs, ControlEvent evt) : base(rhs)
        {
            type = "CONTROL";
            m_Control = evt;
        }
        public ControlEvent Control
        {
            get => m_Control;
            set => m_Control = value;
        }
        public ControlType Action => Enum.TryParse(m_Control.action, true, out ControlType result) ? result : ControlType.UNKNOWN;

        public override string ToJson
        {
            get
            {
                string json = RemoveTargetFieldInJson(JsonUtility.ToJson(this));
                if (m_Control is ConversationControlEvent convoCtrl)
                    m_ControlJson = JsonUtility.ToJson(convoCtrl);
                else if (m_Control is AudioControlEvent audioCtrl)
                    m_ControlJson = RemoveTargetFieldInJson(JsonUtility.ToJson(audioCtrl));
                else
                    m_ControlJson = RemoveTargetFieldInJson(JsonUtility.ToJson(m_Control));
                json = Regex.Replace(json, @"(?=\}$)", $",\"control\": {m_ControlJson}");
                return json;
            }
        }
    }
}
