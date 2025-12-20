using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Nest;
using System;
using System.Linq;
using static HlidacStatu.Lib.Web.UI.TagHelpers.ToggleableTagHelper;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("role-limited")]
    [RestrictChildren("pass", "else")]
    public partial class RoleLimitedTagHelper : TagHelper
    {

        public bool HideAnyContent { get; set; } = true;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public bool? AuthenticatedOnly { get; set; } = null;
        public string Roles { get; set; } = "";

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (HideAnyContent)
            {
                output.SuppressOutput(); 
                return;
            }
            var modalContext = new List<SimpleInnerBaseTagRenderHelper.InnerContext>();
            context.Items.Add(typeof(List<SimpleInnerBaseTagRenderHelper.InnerContext>), modalContext);

            var childContent = await output.GetChildContentAsync();
            var innerContent = (List<SimpleInnerBaseTagRenderHelper.InnerContext>)context.Items[typeof(List<SimpleInnerBaseTagRenderHelper.InnerContext>)];


            var passContent = innerContent.FirstOrDefault(m => m.ContentName == "pass").Content;
            var elseContent = innerContent.FirstOrDefault(m => m.ContentName == "else").Content;

            output.TagName = null;

            var user = ViewContext?.HttpContext?.User;
            var userIsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
            var requiredRoles = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(Roles))
                requiredRoles = Roles
                    .Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if ((AuthenticatedOnly == null || AuthenticatedOnly == false)
                && requiredRoles.Length == 0)
            {
                // No restrictions, always show pass content
                output.Content.SetHtmlContent(passContent);
                return;
            }

            if (AuthenticatedOnly == true && requiredRoles.Length == 0)
            {
                if (userIsAuthenticated)
                {
                    output.Content.SetHtmlContent(passContent);
                    return;
                }
                else
                {
                    output.Content.SetHtmlContent(elseContent);
                    return;
                }
            }

            if (requiredRoles.Length > 0)
            {
                if (!userIsAuthenticated)
                {
                    output.Content.SetHtmlContent(elseContent);
                    return;
                }

                var inAnyRole = requiredRoles.Any(r => user.IsInRole(r));
                if (inAnyRole)
                {
                    output.Content.SetHtmlContent(passContent);
                    return;
                }
                else
                {
                    output.Content.SetHtmlContent(elseContent);
                    return;
                }

            }

        }


    }

    [HtmlTargetElement("pass", ParentTag = "role-limited")]
    public class RoleLimited_Pass_TagHelper : SimpleInnerBaseTagRenderHelper
    {
        public override string contentName => "pass";
    }

    [HtmlTargetElement("else", ParentTag = "role-limited")]
    public class RoleLimited_Else_TagHelper : SimpleInnerBaseTagRenderHelper
    {
        public override string contentName => "else";
    }


    public abstract class SimpleInnerBaseTagRenderHelper : TagHelper
    {
        public class InnerContext
        {
            public int Num { get; set; }
            public string ContentName { get; set; }

            public string Content { get; set; }
        }

        public static string GetContentFromContext(TagHelperContext context, string contentName)
        {
            if (context.Items.TryGetValue(typeof(SimpleInnerBaseTagRenderHelper).Name + $"_{contentName}_content", out var contentObj))
            {
                return contentObj as string ?? "";
            }
            return "";
        }

        public abstract string contentName { get; }

        protected List<InnerContext> toggleableContent = null;


        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = (await output.GetChildContentAsync()).GetContent();

            //context.Items.TryAdd(typeof(SimpleInnerBaseTagRenderHelper).Name + $"_{this.contentMode}_content", content);

            toggleableContent = (List<InnerContext>)context.Items[typeof(List<InnerContext>)];

            toggleableContent.Add(new InnerContext() { ContentName = contentName, Content = content, Num = toggleableContent.Count });


            output.SuppressOutput(); // Prevent outputting the tag itself.
        }

    }

}