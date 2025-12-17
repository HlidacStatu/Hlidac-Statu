using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("honeypot")]
    public class HoneypotTagHelper : TagHelper
    {
        public const string AntispamInputName = "zip_2";

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public string Name { get; set; } = AntispamInputName;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

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