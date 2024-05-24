/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

namespace Inworld.Sample
{
    public class PlayerController2D : PlayerController
    {
        protected void Start()
        {
            if (m_SendButton)
                m_SendButton.interactable = false;
            if (m_RecordButton)
                m_RecordButton.interactable = false;
        }
        protected override void OnCharacterJoined(InworldCharacter newChar)
        {
            if (m_SendButton)
                m_SendButton.interactable = true;
            if (m_RecordButton)
                m_RecordButton.interactable = true;
        }
        protected override void OnCharacterLeft(InworldCharacter newChar)
        {
            if (m_SendButton)
                m_SendButton.interactable = InworldController.CurrentCharacter;
            if (m_RecordButton)
                m_RecordButton.interactable = InworldController.CurrentCharacter;
        }
    }
}

