using HlidacStatu.Lib.Web.UI;
using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    public class LoginLinkTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string returnPage = System.Net.WebUtility.UrlEncode(ViewContext.GetRequestPathAndQuery());
            string registerUrl = $"/Identity/Account/Register?returnUrl={returnPage}";
            string loginUrl = $"/Identity/Account/Login?returnUrl={returnPage}";

            output.TagName = "div";
            output.Content.SetHtmlContent(
                $"<a href=\"{registerUrl}\">Registrace</a> je jednoduchá a zdarma. Pokud už jste zaregistrováni, stačí se <a href=\"{loginUrl}\">přihlásit</a>.");

        }
    }
}