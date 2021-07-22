using System.Net;
using HlidacStatu.Web.Framework;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class ShareFacebookTagHelper: TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
        
        public string Title { get; set; } = "na Facebook";
        
        public string? Url { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string url = string.IsNullOrWhiteSpace(Url) ? ViewContext.GetDisplayUrl() : Url;
            string encUrl = WebUtility.UrlEncode(url);
            
            output.TagName = null;
            output.Content.SetHtmlContent(
                $"<a href=\"https://www.facebook.com/sharer/sharer.php?sdk=joey&u={encUrl}&display=popup&ref=plugin&src=share_button\" class=\"ssk ssk-text ssk-facebook ssk-xs\" target=\"_blank\">{Title}</a>");
        }
    }
}