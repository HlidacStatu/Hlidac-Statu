using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace HlidacStatu.Web.TagHelpers
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
            var toggleableContent  = (List<ToggleableContext>)context.Items[typeof(List<ToggleableContext>)];

            if (toggleableContent.Count > 2)
            {
                output.TagName = null;
                output.Content.SetHtmlContent("<div>Jsou mozne pouze 2 bloky 'content'</div>");
                return;
            }

            // Generate a unique identifier for CSS/JS selectors.
            string random = Guid.NewGuid().ToString("N");
            var sb = new StringBuilder();

            // Build the script to wire up the toggling behavior.
            sb.AppendLine("<script>");
            sb.AppendLine("$(function () {");
            sb.AppendLine($"  $('.{random}_first.btn').click(function () {{");
            sb.AppendLine($"      $('.{random}_first.content').show();");
            sb.AppendLine($"      $('.{random}_second.content').hide();");
            sb.AppendLine($"      $('.{random}_first.btn').addClass('btn-primary');");
            sb.AppendLine($"      $('.{random}_second.btn').removeClass('btn-primary');");
            sb.AppendLine("  });");
            sb.AppendLine($"  $('.{random}_second.btn').click(function () {{");
            sb.AppendLine($"      $('.{random}_first.content').hide();");
            sb.AppendLine($"      $('.{random}_second.content').show();");
            sb.AppendLine($"      $('.{random}_first.btn').removeClass('btn-primary');");
            sb.AppendLine($"      $('.{random}_second.btn').addClass('btn-primary');");
            sb.AppendLine("  });");
            sb.AppendLine("});");
            sb.AppendLine("</script>");

            var i1 = toggleableContent[0];
            var i2 = toggleableContent[1];

            // Build the toggle buttons and the content areas.
            sb.AppendLine($"<div class='btn btn-default {random}_first btn-primary' " +
                          $"style='border-top-right-radius: 0px;border-bottom-right-radius: 0px;'>{i1.ButtonText}</div>");
            sb.AppendLine($"<div class='btn btn-default {random}_second' " +
                          $"style='border-top-left-radius: 0px;border-bottom-left-radius: 0px;'>{i2.ButtonText}</div>");
            sb.AppendLine($"<div class='{random}_first content'>{i1.Content}</div>");
            sb.AppendLine($"<div class='{random}_second content' style='display: none;'>{i2.Content}</div>");


            // Remove the outer tag and set the generated content.
            output.TagName = null;
            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    //[HtmlTargetElement("content", ParentTag = "toggleable")]
    //public class ToggleableInner1TagHelp : ToggleableInnerBaseTagHelper
    //{
    //    public override void AssignValues(string buttonName, string content)
    //    {
    //        this.toggleableContent.ButtonText1 = buttonName;
    //        this.toggleableContent.Content1 = content;
    //    }
    //}

    //[HtmlTargetElement("content-two", ParentTag = "toggleable")]
    //public class ToggleableInner2TagHelp : ToggleableInnerBaseTagHelper
    //{
    //    public override void AssignValues(string buttonName, string content)
    //    {
    //        this.toggleableContent.ButtonText2 = buttonName;
    //        this.toggleableContent.Content2 = content;
    //    }
    //}


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