using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("hidecontent")]
    public class HideContentTagHelper : TagHelper
    {
        public int? Height { get; set; } = 120;



        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var preContent = $@"<div class='low-box' style='max-height:{Height}px'>
        <div class='low-box-line' style='top:{Height - 55}px'></div>
        <div class='low-box-content'>";

            output.TagName = null;
            output.PreContent.SetHtmlContent(preContent);
            output.PostContent.SetHtmlContent("</div></div>");

        }

    }
}