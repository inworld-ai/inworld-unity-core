/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System.Collections.Generic;

namespace Inworld.Entities.LLM
{
	public class ContentItem
	{
		
	}
	public class ImageUrl
	{
		// URL of the image.
		public string url;
		// Specifies the detail level of the image: 'low', 'high', 'auto'. Defaults to 'auto'.
		// More info https://platform.openai.com/docs/guides/vision
		public string detail;
	}
	public class TextItem : ContentItem
	{
		// Text content of the message item.
		public string text;
	}
	public class ImageItem : ContentItem
	{
		// URL of the image content of the message item.
		public ImageUrl image_url;
	}
	public class ContentItems
	{
		public List<ContentItem> content_items;
	}
}
