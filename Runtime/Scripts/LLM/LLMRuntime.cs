/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using UnityEngine;

namespace Inworld.LLM
{
	public class LLMRuntime : MonoBehaviour
	{
		protected InworldConnectionStatus m_Status;
		
#region Events 
		// TODO(Yan): Split the InworldAuth to separate MonoBehaviour.
		public event Action<InworldConnectionStatus> OnStatusChanged;
#endregion
		public virtual InworldConnectionStatus Status
		{
			get => m_Status;
			set
			{
				if (m_Status == value)
					return;
				m_Status = value;
				OnStatusChanged?.Invoke(value);
			}
		}
		public virtual void SendText(string text)
		{
			
		}
	}
}
