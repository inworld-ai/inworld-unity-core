/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using Inworld.Entities;
using Inworld.UI;
using UnityEngine;

namespace Inworld.Sample
{
    public class CharSelectorPanel : BubblePanel
    {

        [SerializeField] CharacterButton m_CharSelectorPrefab;

        public override bool IsUIReady => base.IsUIReady  && m_CharSelectorPrefab;
        void OnEnable()
        {
            InworldController.Client.OnSessionUpdated += SessionUpdated;
        }

        void OnDisable()
        {
            if (!InworldController.Instance)
                return;
            InworldController.Client.OnSessionUpdated -= SessionUpdated;
        }



        void SessionUpdated(InworldCharacterData charData)
        {
            if (!IsUIReady)
                return;
            InsertBubble(charData.brainName, m_CharSelectorPrefab, charData.givenName);
            StartCoroutine((m_Bubbles[charData.brainName] as CharacterButton)?.SetData(charData));
        }
    }
}
