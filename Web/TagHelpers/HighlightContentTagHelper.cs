using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class HighlightContentTagHelper : TagHelper
    {
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlights { get; set; }
        public string Path { get; set; }
        public string ContentToCompare { get; set; }
        public string? FoundContentFormat { get; set; } = null;
        public string NoHLContent { get; set; } = "";
        public string Prefix { get; set; } = "";
        public string Postfix { get; set; } = "";
        public string HighlightPartDelimiter { get; set; } = " ..... ";
        public string Icon { get; set; } = "far fa-search";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

            var result = Repositories.Searching.Highlighter.HighlightContentIntoHtmlBlock(
                Highlights, Path, ContentToCompare,
                FoundContentFormat, NoHLContent, Prefix,
                Postfix, HighlightPartDelimiter, Icon);

            output.TagName = null;
            output.Content.SetHtmlContent(result);

        }
    }
}