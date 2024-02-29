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
		
		void OnEnable()
		{
			InworldController.Client.OnStatusChanged += OnStatusChanged;
		}

		void OnDisable()
		{
			if (!InworldController.Instance)
				return;
			InworldController.Client.OnStatusChanged -= OnStatusChanged;
		}
		void OnStatusChanged(InworldConnectionStatus incomingStatus)
		{
			m_Board.SetActive(incomingStatus != InworldConnectionStatus.Idle && incomingStatus != InworldConnectionStatus.Connected);
			m_Indicator.text = incomingStatus.ToString();
		}
	}
}
