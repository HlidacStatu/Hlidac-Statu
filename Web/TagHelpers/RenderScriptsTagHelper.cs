using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

using System;
using System.Collections.Generic;

namespace HlidacStatu.Web.TagHelpers
{
    public class RenderScriptsTagHelper : TagHelper
    {
        private const string Key = "queued-scripts";

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!ViewContext.ViewData.TryGetValue(Key, out var scriptQueueObj))
            {
                output.SuppressOutput();
                return;
            }

            var scriptQueue = (Queue<string>)scriptQueueObj;

            var scripts = string.Join("\n", scriptQueue);
            output.Content.SetHtmlContent(scripts);
        }

        public static void QueueScript(ViewContext viewContext, string script)
        {
            if (viewContext == null) throw new ArgumentNullException(nameof(viewContext));
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(script))
            {
                return;
            }

            Queue<string> scriptQueue = new();

            if (viewContext.ViewData.TryGetValue(Key, out var scriptQueueObj))
            {
                scriptQueue = (Queue<string>)scriptQueueObj;
            }
            else
            {
                viewContext.ViewData.Add(Key, scriptQueue);
            }

            scriptQueue.Enqueue(script);
        }
    }
}