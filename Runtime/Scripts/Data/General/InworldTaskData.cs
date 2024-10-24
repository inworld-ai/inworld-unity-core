/*************************************************************************************************
 * Copyright 2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;

namespace Inworld.Data
{
    [Serializable]
    public class ListTaskResponse
    {
        public List<InworldTaskData> customTasks;
    }
    
    [Serializable]
    public class InworldTaskData
    {
        public string ShortName => name.Substring(name.LastIndexOf('/') + 1);
        
        public string name;
        public List<TaskParameter> parameters;
    }

    [Serializable]
    public class TaskParameter
    {
        public string name;
        public string description;
    }
}
