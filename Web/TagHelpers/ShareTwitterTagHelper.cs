using System.Net;
using HlidacStatu.Web.Framework;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class ShareTwitterTagHelper: TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
        
        public string Text { get; set; }

        public string Title { get; set; } = "na Twitter";
        
        public string? Url { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string url = string.IsNullOrWhiteSpace(Url) ? ViewContext.GetDisplayUrl() : Url;
            string encUrl = WebUtility.UrlEncode(url);
            
            string encText = WebUtility.UrlEncode(Text);
            
            output.TagName = null;
            output.Content.SetHtmlContent(
                $"<a href=\"https://twitter.com/intent/tweet?original_referer={encUrl}&ref_src=twsrc%5Etfw&text={encText}&tw_p=tweetbutton&url={encUrl}\" class=\"ssk ssk-text ssk-twitter ssk-xs\" target=\"_blank\">{Title}</a>");
        }
    }
}