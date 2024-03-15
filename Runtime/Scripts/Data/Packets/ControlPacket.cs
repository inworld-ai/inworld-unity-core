/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using UnityEngine;

namespace Inworld.Packet
{
    [Serializable]
    public class ControlEvent
    {
        public string action;
        public string description;
    }
    [Serializable]
    public class ControlPacket : InworldPacket
    {
        public ControlEvent control;

        public ControlPacket()
        {
            type = "CONTROL";
            control = new ControlEvent();
        }
        public ControlPacket(InworldPacket rhs, ControlEvent evt) : base(rhs)
        {
            type = "CONTROL";
            control = evt;
        }
        public ControlType Action => Enum.TryParse(control.action, true, out ControlType result) ? result : ControlType.UNKNOWN;
        public override string ToJson => JsonUtility.ToJson(this); 
    }
}
