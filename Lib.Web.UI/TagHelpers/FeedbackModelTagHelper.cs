using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{

    /// <summary>
    /// add this code into program.cs before app.Run();
    /// // --- Minimal API endpoint pro FeedbackModalTagHelper ---
    /// _ = app.MapPost(FeedbackModalTagHelper.AcceptDataUrl, FeedbackModalTagHelper.AcceptDataDelegate );

    /// </summary>
    [HtmlTargetElement("feedback-modal")]
    public partial class FeedbackModalTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FeedbackModalTagHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        [HtmlAttributeName("button-text")]
        public string? ButtonText { get; set; } = "Máte chybu nebo něco chybí";

        [HtmlAttributeName("button-style")]
        public string? ButtonStyle { get; set; }

        [HtmlAttributeName("must-auth")]
        public bool MustAuth { get; set; } = false;

        [HtmlAttributeName("add-data")]
        public string? AddData { get; set; }

        [HtmlAttributeName("select-option")]
        public string? SelectOption { get; set; }

        [HtmlAttributeName("options")]
        public string[]? Options { get; set; }

        [HtmlAttributeName("id-prefix")]
        public string? IdPrefix { get; set; }

        [HtmlAttributeName("show-default-alert")]
        public bool ShowDefaultAlert { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // Don't render the tag itself
            output.TagMode = TagMode.StartTagAndEndTag;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                output.SuppressOutput();
                return;
            }

            // Set defaults
            var buttonStyle = ButtonStyle ?? "btn btn-primary btn-sm";
            var idPrefix = IdPrefix ?? $"m{Guid.NewGuid():N}";
            var options = Options ?? GetDefaultOptions();
            var currentUrl = httpContext.Request.GetDisplayUrl();
            var userEmail = httpContext.User?.Identity?.Name ?? "";
            var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;

            bool useDefaultForm = Options == null;

            // Check authentication requirement
            if (MustAuth && !isAuthenticated)
            {
                GenerateUnauthenticatedModal(output, buttonStyle, idPrefix, currentUrl);
            }
            else
            {
                GenerateAuthenticatedModal(output, buttonStyle, idPrefix, options, currentUrl, userEmail, useDefaultForm);
            }
        }

        private void GenerateAuthenticatedModal(TagHelperOutput output, string buttonStyle, string idPrefix,
            string[] options, string currentUrl, string userEmail, bool useDefaultForm)
        {
            var html = $@"
<!-- Button trigger modal -->
<button class=""{HtmlEncoder.Default.Encode(buttonStyle)}"" data-bs-toggle=""modal"" data-bs-target=""#{idPrefix}fbForm"">{HtmlEncoder.Default.Encode(ButtonText ?? "")}</button>

<!-- Modal -->
<div class=""modal fade"" id=""{idPrefix}fbForm"" tabindex=""-1"" role=""dialog""
     aria-labelledby=""{idPrefix}fbTitle"" aria-hidden=""true"">
    <div class=""modal-dialog"">
        <div class=""modal-content"">
            <!-- Modal Header -->
            <div class=""modal-header"">
                <h4 class=""modal-title"" id=""{idPrefix}fbTitle"">
                    Upozornění na chybu Hlídače státu či poslání vzkazu
                </h4>
                <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"">
                    <span class=""sr-only"">Close</span>
                </button>
            </div>

            <!-- Modal Body -->
            <div class=""modal-body"">";

            if (useDefaultForm && ShowDefaultAlert)
            {
                html += @"
                <div class=""alert alert-danger"" role=""alert"">
                    Opravy údajů u smlouvy je možné provádět pouze přes <a href=""https://portal.gov.cz/registr-smluv/formulare"" target=""_blank"">formuláře registru smluv</a>
                    <br/>
                    My pouze přebíráme údaje v registru smluv. Měnit je nemůžeme.
                </div>";
            }

            html += $@"
                <form class=""form-horizontal"" role=""form"">
                    <input type=""hidden"" id=""{idPrefix}fbdata"" name=""{idPrefix}fbdata"" value=""{HtmlEncoder.Default.Encode(AddData ?? "")}""/>
                    <input type=""hidden"" name=""{idPrefix}fbUrl"" id=""{idPrefix}fbUrl"" value=""{HtmlEncoder.Default.Encode(currentUrl)}""/>
                    
                    <div class=""form-group"">
                        <label class=""col-sm-2 control-label"" for=""{idPrefix}fbInputTyp"">
                            Typ zprávy
                        </label>
                        <div class=""col-sm-10"">
                            <select class=""form-control"" id=""{idPrefix}fbInputTyp"">";

            // Generate options
            for (int i = 0; i < options.Length; i += 2)
            {
                var value = options[i];
                var text = i + 1 < options.Length ? options[i + 1] : value;
                var selected = value == SelectOption ? " selected=\"selected\"" : "";
                html += $@"<option{selected} value=""{HtmlEncoder.Default.Encode(value)}"">{HtmlEncoder.Default.Encode(text)}</option>";
            }

            html += $@"
                            </select>
                        </div>
                    </div>
                    <div class=""form-group"">
                        <label class=""col-sm-2 control-label"" for=""{idPrefix}fbInputTxt"">
                            Vzkaz
                        </label>
                        <div class=""col-sm-10"">
                            <textarea class=""form-control"" id=""{idPrefix}fbInputTxt"" cols=""60"" rows=""5"" placeholder=""Text zprávy""></textarea>
                        </div>
                    </div>
                    <div class=""form-group"">
                        <label class=""col-sm-2 control-label"" for=""{idPrefix}fbInputEmail"">
                            Váš e-mail
                        </label>
                        <div class=""col-sm-10"">
                            <input type=""email"" class=""form-control"" id=""{idPrefix}fbInputEmail"" placeholder=""Email"" value=""{HtmlEncoder.Default.Encode(userEmail)}""/>
                        </div>
                    </div>
                </form>
            </div>

            <!-- Modal Footer -->
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-default"" data-bs-dismiss=""modal"">
                    Zrušit
                </button>
                <button type=""button"" onclick=""send{idPrefix}();"" class=""btn btn-primary"" data-bs-dismiss=""modal"">
                    Odeslat vzkaz
                </button>
                <br/>
                <div>
                    <a class=""btn btn-default btn-sm"" href=""/Kontakt"">Další kontakty zde</a>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
function send{idPrefix}() {{
    var prf = '#{idPrefix}';
    var typ = $(prf + ""fbInputTyp"").val();
    var email = $(prf + ""fbInputEmail"").val();
    var text = $(prf + ""fbInputTxt"").val();
    var url = $(prf + ""fbUrl"").val();
    var adddata = $(prf + ""fbdata"").val();
    $.post(""/__FeedbackModal/submit"", {{ typ: typ, email: email, txt: text, url: url, data: adddata, auth: {MustAuth.ToString().ToLower()} }})
      .always(function () {{
          alert(""Děkujeme za zaslání zprávy."");
      }});
}}
</script>";

            output.Content.SetHtmlContent(html);
        }

        private void GenerateUnauthenticatedModal(TagHelperOutput output, string buttonStyle, string idPrefix, string currentUrl)
        {
            var html = $@"
<!-- Button trigger modal -->
<button class=""{HtmlEncoder.Default.Encode(buttonStyle)}"" data-bs-toggle=""modal"" data-bs-target=""#{idPrefix}fbForm"">{HtmlEncoder.Default.Encode(ButtonText ?? "")}</button>

<!-- Modal -->
<div class=""modal fade"" id=""{idPrefix}fbForm"" tabindex=""-1"" role=""dialog""
     aria-labelledby=""{idPrefix}fbTitle"" aria-hidden=""true"">
    <div class=""modal-dialog"">
        <div class=""modal-content"">
            <!-- Modal Header -->
            <div class=""modal-header"">
                <h4 class=""modal-title"" id=""{idPrefix}fbTitle"">
                    Musíte být přihlášeni
                </h4>
                <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"">
                    <span class=""sr-only"">Close</span>
                </button>
            </div>

            <!-- Modal Body -->
            <div class=""modal-body"">
                <form class=""form-horizontal"" role=""form"">
                    <p>Pro poslání vzkazu musíte být přihlášeni.</p>
                    <p>
                        <a href=""https://www.hlidacstatu.cz/Identity/Account/Login?returnUrl={currentUrl}"">Přihlásit se</a>
                    </p>
                </form>
            </div>

            <!-- Modal Footer -->
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-default"" data-bs-dismiss=""modal"">
                    Zavřít
                </button>
            </div>
        </div>
    </div>
</div>";

            output.Content.SetHtmlContent(html);
        }

        private string[] GetDefaultOptions()
        {
            return new string[]
            {
                "Chyba", "Chci upozornit na chybu",
                "Afera", "Tuhle aféru byste měli sledovat",
                "Stiznost", "Chci si stěžovat",
                "Pochvala", "Chci vás pochválit",
                "NabidkaPomoci", "Nabízím vám pomoc",
                "Jiné", "Jiné",
            };
        }
    }
}

// Usage Examples in Views:

// Basic usage with defaults
// <feedback-modal button-text="Nahlásit problém"></feedback-modal>

// Advanced usage with custom options
// <feedback-modal 
//     button-text="Zpětná vazba" 
//     button-style="btn btn-warning btn-lg"
//     must-auth="true"
//     add-data="some-additional-data"
//     select-option="Chyba"
//     id-prefix="custom"
//     show-default-alert="false">
// </feedback-modal>

// Don't forget to register the TagHelper in _ViewImports.cshtml:
// @addTagHelper *, YourLibrary