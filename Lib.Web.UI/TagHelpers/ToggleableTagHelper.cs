using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace HlidacStatu.Lib.Web.UI.TagHelpers
{

    [HtmlTargetElement("toggleable")]
    [RestrictChildren("content")]
    public class ToggleableTagHelper : TagHelper
    {
        public class ToggleableContext
        {
            public int Num { get; set; }
            public string ButtonText { get; set; }

            public string Content { get; set; }
        }


        // Allows access to ViewContext if needed
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var modalContext = new List<ToggleableContext>();
            context.Items.Add(typeof(List<ToggleableContext>), modalContext);
            var childContent = await output.GetChildContentAsync();
            var toggleableContent = (List<ToggleableContext>)context.Items[typeof(List<ToggleableContext>)];

            //if (toggleableContent.Count > 2)
            //{
            //    output.TagName = null;
            //    output.Content.SetHtmlContent("<div>Jsou mozne pouze 2 bloky 'content'</div>");
            //    return;
            //}

            // Generate a unique identifier for CSS/JS selectors.
            string random = Guid.NewGuid().ToString("N");

            // Build the script to wire up the toggling behavior.
            var sb = new StringBuilder(1024);

            sb.AppendLine(@"<div class='hstabs'>");

            sb.AppendLine(@"<div class='tab-buttons'>");
            foreach (var item in toggleableContent)
            {
                sb.AppendLine($"<div class='btn btn-secondary {(item.Num == 0 ? "btn-primary" : "" )}' data-tab='tab{item.Num}' ");
                sb.AppendLine($" style='border-top-right-radius: 0px; border-bottom-right-radius: 0px;'>");
                sb.AppendLine(item.ButtonText);
                sb.AppendLine($"</div>");
            }
            sb.AppendLine(@" </div>");
            //Tab Content -->
            sb.AppendLine(@"<div class='tab-content'>");
            foreach (var item in toggleableContent)
            {
                sb.AppendLine($"<div class='content' data-tab='tab{item.Num}' style='display: {(item.Num == 0 ? "block" : "none")};'>");
                sb.AppendLine(item.Content);
                sb.AppendLine($"</div>");
            }
                sb.AppendLine(@"</div>");
            sb.AppendLine(@"</div>");


            // Remove the outer tag and set the generated content.
            output.TagName = null;
            output.Content.SetHtmlContent(sb.ToString());
        }
    }




    [HtmlTargetElement("content", ParentTag = "toggleable")]
    public class ToggleableInnerBaseTagHelper : TagHelper
    {
        [HtmlAttributeName("buttontext")]
        public string ButtonText { get; set; }

        protected List<ToggleableTagHelper.ToggleableContext> toggleableContent = null;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = (await output.GetChildContentAsync()).GetContent();

            toggleableContent = (List<ToggleableTagHelper.ToggleableContext>)context.Items[typeof(List<ToggleableTagHelper.ToggleableContext>)];

            toggleableContent.Add(new ToggleableTagHelper.ToggleableContext() { ButtonText = ButtonText, Content = content, Num = toggleableContent.Count });
            output.SuppressOutput(); // Prevent outputting the tag itself.
        }

    }
}