﻿/*************************************************************************************************
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
    public class GestureEvent
    {
        public string type;
        public string playback;
    }
    [Serializable]
    public class GesturePacket : InworldPacket
    {
        public GestureEvent gesture;
        
        public GesturePacket()
        {
            type = "GESTURE";
            gesture = new GestureEvent();
        }
        public GesturePacket(InworldPacket rhs, GestureEvent evt) : base(rhs)
        {
            type = "GESTURE";
            gesture = evt;
        }
        public override string ToJson => JsonUtility.ToJson(this); 
    }
}
