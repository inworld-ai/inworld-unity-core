/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using UnityEngine;

namespace Inworld.Entities
{
    [Serializable]
    public class Token
    {
        public string token;
        public string type;
        public string expirationTime;
        public string sessionId;
        
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(token))
                    return false;
                if (string.IsNullOrEmpty(type))
                    return false;
                Debug.Log($"YAN UTC Now: {DateTime.UtcNow} TokenTime: {InworldDateTime.ToDateTime(expirationTime)}");
                return DateTime.UtcNow < InworldDateTime.ToDateTime(expirationTime);
            }
        }
    }
    [Serializable]
    public class AccessTokenRequest
    {
        public string api_key;
        public string resource_id;
    }
}
