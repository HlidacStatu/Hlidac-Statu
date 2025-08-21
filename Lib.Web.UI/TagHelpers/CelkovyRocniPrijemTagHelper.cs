using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("celkovy-rocni-prijem", TagStructure = TagStructure.WithoutEndTag)]
    public sealed class CelkovyRocniPrijemTagHelper : TagHelper
    {
        public const string Content =
            "Celkový roční příjem <i class=\"fa-solid fa-circle-info\" data-bs-toggle=\"tooltip\" data-bs-title=\"Celkový roční hrubý příjem, který zahrnuje vše (plat, odměny, bonusy, příspěvky, účelové i neúčelové náhrady).\" aria-hidden=\"true\"></i>";
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            // If user provided class -> use it. Otherwise -> default "text-nowrap".
            string classes = context.AllAttributes.TryGetAttribute("class", out var classAttr)
                ? classAttr.Value?.ToString() ?? "text-nowrap"
                : "text-nowrap";
            
            output.Attributes.SetAttribute("class", classes);

            output.Content.SetHtmlContent(Content);
        }
    }
}