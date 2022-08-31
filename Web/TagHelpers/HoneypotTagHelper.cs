using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class HoneypotTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public string Name { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = Framework.Constants.AntispamInputName;

            output.TagName = null;
            output.Content.SetHtmlContent($@"<input type='text' name='{Name}' id='{Name}' value='' autocomplete='off' placeholder='Your Zip code' />");


            RenderScriptsTagHelper.QueueScript(ViewContext,
                @"<script>
                    $(function () {
                        $('#" + Name + @"').css({ ""font-size"": ""1pt"", ""color"": ""white"", ""width"": ""1px"", ""border"": ""none""});
                    });    
                </script>
                <noscript>
                    <style>
                        #" + Name + @" {
                            font-size: 1pt; color: white; width:1px; border:none;
                        }
                    </style>
                </noscript>
            ");

        }
    }
}