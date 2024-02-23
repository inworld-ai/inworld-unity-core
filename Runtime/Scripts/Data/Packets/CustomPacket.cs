/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
namespace Inworld.Packet
{
    [Serializable]
    public class TriggerParameter
    {
        public string name;
        public string value;
    }
    [Serializable]
    public class CustomEvent
    {
        public string name;
        public List<TriggerParameter> parameters;

        public CustomEvent()
        {
            name = "";
            parameters = new List<TriggerParameter>();
        }

        public CustomEvent(string eventName, Dictionary<string, string> eventParameters)
        {
            name = eventName;
            if (eventParameters != null)
                parameters = eventParameters.Select
                (
                    parameter =>
                        new TriggerParameter
                        {
                            name = parameter.Key,
                            value = parameter.Value
                        }
                ).ToList();
        }
    }
    [Serializable]
    public class CustomPacket : InworldPacket
    {
        public CustomEvent custom;

        public CustomPacket()
        {
            type = "CUSTOM";
            custom = new CustomEvent();
        }
        public CustomPacket(InworldPacket rhs, CustomEvent evt) : base(rhs)
        {
            type = "CUSTOM";
            custom = evt;
        }
        public string TriggerName
        {
            get
            {
                switch (Message)
                {
                    case InworldMessage.GoalComplete:
                        return custom.name.Substring(InworldMessenger.GoalCompleteHead);
                    case InworldMessage.None:
                        return custom.name;
                }
                return "";
            }
        }

        public string Trigger
        {
            get
            {
                string result = TriggerName;
                if (custom.parameters == null || custom.parameters.Count == 0)
                    return result;
                foreach (TriggerParameter param in custom.parameters)
                {
                    result += $" {param.name}: {param.value} ";
                }
                return result;
            }
        }
        public InworldMessage Message => InworldMessenger.ProcessPacket(this);
        
        public override string ToJson => JsonUtility.ToJson(this); 
    }
}
