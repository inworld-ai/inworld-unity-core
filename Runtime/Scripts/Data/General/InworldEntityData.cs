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
    public class ListEntityResponse
    {
        public List<InworldEntityData> entities;
    }
    
    [Serializable]
    public class InworldEntityData
    {
        public string name;
        public string displayName;
        public string description;
        public List<TaskReference> customTasks;
    }

    [Serializable]
    public class TaskReference
    {
        public string ShortName => task.Substring(task.LastIndexOf('/') + 1);
        
        public string task;
        public string parameter;
    }
}
