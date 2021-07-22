using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class LowBoxTagHelper : TagHelper
    {
        public int? Width { get; set; } = 120;
        
        public string? GaPageEventId { get; set; }
        
        
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var preContent = $@"<div class='low-box' style='max-height:{Width}px'>
        <div class='low-box-line' style='top:{Width - 55}px'>
        <a href='#' onclick=""ga('send', 'event', 'btnLowBoxMore', 'showMore','{GaPageEventId}'); return true;"" class='more'></a></div>
        <div class='low-box-content'>";

            output.TagName = null;
            output.PreContent.SetHtmlContent(preContent);
            output.PostContent.SetHtmlContent("</div></div>");
            
        }

    }
}