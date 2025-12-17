using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Linq;
using static HlidacStatu.Lib.Web.UI.TagHelpers.ToggleableTagHelper;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("role-limited")]
    [RestrictChildren("pass", "else")]

    public partial class RoleLimitedTagHelper : TagHelper
    {


        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public bool? AuthenticatedOnly { get; set; } = null;
        public string Roles { get; set; } = "";

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
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

}