using HlidacStatu.Entities.KIndex;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    [HtmlTargetElement("parametr-description", TagStructure = TagStructure.WithoutEndTag)]
    public class ParametrDescriptionTagHelper: TagHelper
    {
        public KIndexData.Annual Data { get; set; }
        public KIndexData.KIndexParts Part { get; set; }
        public bool Autohide { get; set; } = true;
        public string CustomStyle { get; set; } = "";

        public string CustomClass { get; set; } = "bs-callout info small";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            
            string descr = "";
            descr = Data.Info(Part).Description;

            string result = "";
            if (!string.IsNullOrEmpty(descr))
            {
                string classProp = Autohide ? "collapse helpKidx" : "";
                result = $@"<div class=""{classProp}"" aria-expanded=""true"">
                <div class=""{CustomClass}"" style=""{CustomStyle}"">{descr}</div></div>";

            }
            
            output.TagName = null;
            output.Content.SetHtmlContent(result);
        }

    }
}