using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PlatyUredniku.Views.Shared.TagHelpers;

[HtmlTargetElement("hashtag", Attributes = "tag")]
public class HashTag : TagHelper
{
    public string Tag { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;

        output.Attributes.SetAttribute("href", $"Oblast/{Tag}");
        output.Content.AppendHtml($"<i class=\"fa-solid fa-hashtag\"></i>{Tag}");
    }
}