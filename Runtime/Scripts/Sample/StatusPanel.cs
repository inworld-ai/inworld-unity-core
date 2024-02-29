/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using TMPro;
using UnityEngine;

namespace Inworld.Sample
{
	public class StatusPanel: MonoBehaviour
	{
		[SerializeField] GameObject m_Board;
		[SerializeField] TMP_Text m_Indicator;
		[SerializeField] TMP_Text m_Error;
		
		protected virtual void OnEnable()
		{
			InworldController.Client.OnStatusChanged += OnStatusChanged;
		}

		protected virtual void OnDisable()
		{
			if (!InworldController.Instance)
				return;
			InworldController.Client.OnStatusChanged -= OnStatusChanged;
		}
		void OnStatusChanged(InworldConnectionStatus incomingStatus)
		{
			m_Board.SetActive(incomingStatus != InworldConnectionStatus.Idle && incomingStatus != InworldConnectionStatus.Connected);
			if (m_Indicator)
				m_Indicator.text = incomingStatus.ToString();
			if (m_Error && incomingStatus == InworldConnectionStatus.Error)
				m_Error.text = InworldController.Client.Error;
		}
	}
}
