using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PlatyUredniku.Views.Shared.TagHelpers;

[HtmlTargetElement("hashtag", Attributes = "tag")]
public class HashTag : TagHelper
{
    public string Tag { get; set; }
    public string AdditionalClass { get; set; } = "badge rounded-pill ";
    public string Style { get; set; }
    public int? Count { get; set; } = null;

    public PlatyUredniku.Bootstrap.Colors Color { get; set; } = Bootstrap.Colors.Light;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var style = " " + this.Style;
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Count.HasValue)
            this.AdditionalClass += " position-relative me-3 mb-2";

        output.Attributes.SetAttribute("href", $"Oblast/{Tag}");
        output.Attributes.SetAttribute("class", $"hashtag text-bg-{this.Color.ToString().ToLower()} {AdditionalClass}");
        if (!string.IsNullOrEmpty( Style ) )
        {
            output.Attributes.SetAttribute("style", Style);
        }
        output.Content.AppendHtml(
            $"<i class=\"fa-solid fa-hashtag\"></i>{Tag}"
            + (Count.HasValue ? $"<span class=\"position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger\">{this.Count}</span>" : "")
            );
    }
}