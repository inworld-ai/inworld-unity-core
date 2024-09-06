/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections.Generic;
namespace Inworld.Entities.LLM
{
	// The prompt to generate text from.
	public class Prompt
	{
		// Prompt text.
		public string text;
	}
	// Text completion request.
	public class CompleteTextRequest
	{
		public Serving serving_id; // The serving ID of the request.
		public Prompt prompt; // The prompt to generate text from.
		public TextGenerationConfig text_generation_config; // The generation configuration to use instead of the model's default one.
	}
	// Text completion response.
	public class CompleteTextResponse 
	{
		public string id; // A unique identifier for the completion.
		public List<TextChoice> choices; // The list of completion choices the model generated for the input prompt.
		// The time when the chat completion was created. Can be serialized by InworldDateTime.
		public string create_time;
		// The model used for the chat completion.
		public string model;
		// Usage statistics for the chat completion.
		public Usage usage;
	}
}
