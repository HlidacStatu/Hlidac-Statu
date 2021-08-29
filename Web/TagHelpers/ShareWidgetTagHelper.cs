using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

using System;

namespace HlidacStatu.Web.TagHelpers
{
    public class ShareWidgetTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public string Title { get; set; } = "Vložit do vlastní stránky";
        public int Width { get; set; } = 500;

        public string Url { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var url = string.IsNullOrWhiteSpace(Url) ? ViewContext.GetDisplayUrl() : Url;
            var modalid = Devmasters.TextUtil.GenRandomString(8);
            var wid = Devmasters.TextUtil.GenRandomString(5);
            var pageUrl = new Uri(url);
            var rootUrl = pageUrl.Scheme + "://" + pageUrl.Host;
            var embedUrl = url.Replace(rootUrl, "");
            if (embedUrl.IndexOf("?") > 0)
            {
                embedUrl = embedUrl + "&embed=1";
            }
            else
            {
                embedUrl = embedUrl + "?embed=1";
            }
            var widgetUrl = url.Replace(rootUrl, "");
            var extHtmlL1 = $"<script src='{rootUrl}/widget/{wid}?width={Width}' type='text/javascript'></script>";
            var extHtmlL2 = $"<div id='{wid}' style='width:{Width}px' widget-page='{widgetUrl}'></div>\n";
            var iframeHtml = $"<iframe " +
                             $" style='border: 0px; width: 100%; overflow: hidden; max-width: {Width}px;'" +
                             "frameborder='0' width='100%' border='0' cellspacing='0' " +
                             $"src='{embedUrl}'" +
                             "scrolling='auto' ></iframe>";


            string content = $@"
<span class=""dontembed"">
    <a href="""" class=""ssk ssk-text ssk-amethyst ssk-xs"" data-toggle=""modal"" data-target=""#modal{modalid}"">
        <span class=""glyphicon glyphicon-cog""></span>{Title}
    </a>
</span> <!-- Modal -->
<div class=""modal fade"" id=""modal{modalid}"" tabindex=""-1"" role=""dialog"" aria-labelledby=""modal{modalid}"">
    <div class=""modal-dialog"" role=""document"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <button type=""button"" class=""close"" data-dismiss=""modal"" aria-label=""Close""><span aria-hidden=""true"">&times;</span></button>
                <h4 class=""modal-title"">{Title}</h4>
            </div>
            <div class=""modal-body"">
                <p style=""font-size:14px;"">
                    Přidejte tento HTML kód na svůj web či do článku. <b>Stačí zkopírovat níže uvedený kód</b>:
                </p>
                <form>
                    <textarea id=""modaltxtarea{modalid}"" style=""width: 100%;padding: 7px 9px;margin-bottom: 8px;font-size: 14px;color: #66757f;line-height: 20px;
overflow: hidden;height: 75px;border: 1px solid #ccd6dd;resize: none;
-moz-box-sizing: border-box;box-sizing: border-box;border-radius: 0 3px 3px 3px;display: block;white-space:nowrap;position: relative;z-index: 2;"">{extHtmlL1}
{extHtmlL2}</textarea>
                </form>
                <p style=""font-size:14px;"">
                    Pokud máte <a href=""https://www.michalblaha.cz/2019/01/obsah-hlidace-statu-prehledne-na-vlastni-strance-v-clanku-ci-blogu-kdekoliv/"" target=""_blank"">Wordpress plugin</a>: <code>[hlidac-statu page=""{widgetUrl}""]</code>
                </p>
                <hr />
                <h3>Ukázka zobrazení</h3>
                <div id=""modaliframe{modalid}"">
                    Načítáme náhled, jak to bude vypadat.
                </div>
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-default"" data-dismiss=""modal"">Zavřít</button>
            </div>
        </div>
    </div>
</div>
    <script>
        $(function () {{
            $('#modal{modalid}').on('show.bs.modal', function (event) {{
                var modal = $(this)
                var ifr = modal.find('#modaliframe{modalid}')[0];
                while (ifr.firstChild) {{
                    ifr.removeChild(ifr.firstChild);
                }};
                var div = document.createElement('div');
                div.innerHTML = '{System.Web.HttpUtility.JavaScriptStringEncode(iframeHtml)}';
                ifr.appendChild(div);

                $(""#modaliframe{modalid} iframe"").load(function() {{
                    this.height = this.contentWindow.document.body.scrollHeight;

                }});

            }});
        }});
    </script>";

            output.TagName = null;
            output.Content.SetHtmlContent(content);
        }
    }
}