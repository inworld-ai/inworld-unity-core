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
    public class NarrativeAction
    {
        public string content;
    }
    public class ActionEvent
    {
        public NarrativeAction narratedAction;

        public ActionEvent(string content = "")
        {
            narratedAction = new NarrativeAction()
            {
                content = content
            };
        }
    }
    public sealed class ActionPacket : InworldPacket
    {
        public ActionEvent action;

        public ActionPacket()
        {
            type = PacketType.ACTION;
            action = new ActionEvent();
        }
        public ActionPacket(string actionToSend)
        {
            type = PacketType.ACTION;
            action = new ActionEvent(actionToSend);
            PreProcess();
        }
        public ActionPacket(InworldPacket rhs, ActionEvent evt) : base(rhs)
        {
            action = evt;
            type = PacketType.ACTION;
        }
    }
}
