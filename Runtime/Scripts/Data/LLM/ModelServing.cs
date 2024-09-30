/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;

namespace Inworld.LLM.ModelServing
{
	// Unique identifier of the model being requested.
	[Serializable]
	public class ModelID 
	{
		// The name of the model being served.
		public string model;
		
		// Service provider hosting llm and handling completion requests.
		public ServiceProvider service_provider;
	}
		
	[Serializable]
	public class ServingID
	{
		// ID of the model to use.
		public ModelID model_id;
		
		// Unique identifier representing end-user.
		public string user_id;
		
		// Unique identifier of the session with multiple completion requests.
		public string session_id;
	}
}
