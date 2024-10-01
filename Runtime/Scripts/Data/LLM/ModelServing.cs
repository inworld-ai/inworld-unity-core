/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
using UnityEngine;

namespace Inworld.LLM.ModelServing
{
	// Unique identifier of the model being requested.
	[Serializable]
	public class ModelID 
	{
		[Tooltip("The name of the model being served.")]
		public string model;

		[Tooltip("Service provider hosting llm and handling completion requests")]
		public ServiceProvider service_provider;
		
		public ModelID(string model = "inworld-dragon")
		{
			this.model = model;
			if (model.StartsWith("inworld"))
				service_provider = ServiceProvider.SERVICE_PROVIDER_INWORLD;
			else if (model == "gpt-3.5-turbo-instruct" || model == "gpt-3.5-turbo-0613")
				service_provider = ServiceProvider.SERVICE_PROVIDER_AZURE;
			else
				service_provider = ServiceProvider.SERVICE_PROVIDER_OPENAI;
		}
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
		
		public ServingID(string modelName = "inworld-dragon", string userID = "user-test")
		{
			user_id = userID;
			model_id = new ModelID(modelName);
		}
	}
}
