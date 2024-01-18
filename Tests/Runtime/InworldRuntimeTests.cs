/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
namespace Inworld.Test
{
	public class InworldRuntimeTests
	{
		[UnityTest]
		public IEnumerator InworldRuntimeTest_P()
		{
			Object.Instantiate(InworldAI.ControllerPrefab);
			Assert.NotNull(InworldController.Instance);
			Object.DestroyImmediate(InworldController.Instance);
			Assert.IsNull(InworldController.Instance);
			yield return null;
		}
	}
}
