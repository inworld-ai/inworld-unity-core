/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

namespace Inworld.Entities.LLM
{
	public class Tool
	{
		public class FunctionCall
		{
			public string name;
			public string description;
			public string properties;
		}
		public FunctionCall function_call;
	}

	public class ToolChoice
	{
		public class FunctionCall
		{
			public string name;
		}
		public FunctionCall function_call;
	}
	public class TextToolChoice : ToolChoice
	{
		public string text;
	}
	public class ObjectToolChoice : ToolChoice
	{
		public string @object;
	}
	public class ToolCall
	{
		public class FunctionCall
		{
			public string name;
			public string args;
		}
		public string id;
		public FunctionCall function_call;
	}
}
