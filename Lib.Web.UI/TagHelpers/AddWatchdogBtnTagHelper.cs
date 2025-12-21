using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devmasters.Enums;
using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Entities.VZ;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("watchdog-btn")]
    public sealed class WatchdogTagHelper : TagHelper
    {
        // --- Bindable attributes (nahrazuje properties z původního modelu) ---
        public Type? Datatype { get; set; }
        public string? DatasetId { get; set; }
        public string? Query { get; set; }
        public string? ButtonCss { get; set; }
        public string? ButtonText { get; set; }
        public string? PreButtonText { get; set; }
        public string? PostButtonText { get; set; }
        public bool ShowButtonIcon { get; set; } = true;
        public bool ShowWDList { get; set; } = false;
        public string? PrefillWDname { get; set; }

        // --- Plný přístup k ViewContext (kvůli Request, User, Url apod.) ---
        [ViewContext, HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        private IHttpContextAccessor HttpContextAccessor =>
            ViewContext.HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Tag <watchdog> -> nahradíme čistým HTML
            output.TagName = null;
            var httpCtx = HttpContextAccessor.HttpContext!;
            var user = httpCtx.User;

            // --- Původní logika názvů ---
            var (dataTypeName, dataTypeName2) = GetTypeNames(Datatype);

            var datatypeApi = Datatype?.Name ?? WatchDog.AllDbDataType;
            if (!string.IsNullOrEmpty(DatasetId))
                datatypeApi = $"{datatypeApi}.{DatasetId}";

            // --- periodList (zachování původní logiky Disabled pro Id<2) ---
            var periodList = EnumTools
                .EnumToEnumerable(typeof(WatchDog.PeriodTime))
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Name?.ToString() ?? m.Id.ToString(),
                    Disabled = (Convert.ToInt32(m.Id) < 2)
                })
                .ToList();

            // --- UID pro modály/elementy ---
            var uid = Guid.NewGuid().ToString("N");

            // --- Defaulty tlačítek a textů ---
            var btnCss = string.IsNullOrWhiteSpace(ButtonCss) ? "btn btn-warning" : ButtonCss!;
            var btnText = ButtonText;
            if (string.IsNullOrWhiteSpace(btnText))
            {
                btnText = $"Hlídat nové {dataTypeName}";
                if (!string.IsNullOrEmpty(Query))
                {
                    var qShort = Devmasters.TextUtil.ShortenText(Query!, 30, "...");
                    btnText += $" pro hledání '{qShort}'";
                }
            }

            var preText = PreButtonText;
            if (preText == null)
            {
                preText = $@"
<div class=""section-title"">Hlídání nových {dataTypeName2}</div>
<div class=""flex-row flex-row--center"" style=""margin: 14px 0 16px"">
  <div style=""margin-right: 20px"">
    <img src=""/Content/img/icon-person-watcher.svg"">
  </div>
  <div class=""new-p"">
    Máme pro vás denné sledovat <b>nové {dataTypeName} odpovídající tomuto dotazu</b> a upozornit vás emailem, že se objevily nové?
  </div>
</div>
<div>";
            }

            var postText = PostButtonText ?? "</div>";

            // --- Původně availableWatchdogs = int.MaxValue ---
            var availableWatchdogs = int.MaxValue;

            // --- retUrl_2 pro anonymní modál ---
            var retUrl_2 = System.Net.WebUtility.UrlEncode(httpCtx.Request.GetEncodedPathAndQuery());

            // --- Render ---
            var sb = new StringBuilder(4096);
            sb.Append(preText);

            if (user?.Identity?.IsAuthenticated == true)
            {
                if (availableWatchdogs > 0)
                {
                    sb.Append($@"
<a href=""#"" class=""{btnCss}"" onclick=""_my_event('send','event','btnWatchDog','AddNew','authenticated');""
   role=""button"" data-bs-toggle=""modal"" data-bs-target=""#addWDform{uid}"">");
                    if (ShowButtonIcon)
                        sb.Append(@"<span class=""fas fa-eye dark""></span>");
                    sb.Append($@"<span class=""btn-emphasized"">{btnText}</span></a>");
                }
                else
                {
                    sb.Append($@"
<a href=""/manage/outOfWatchdogs"" class=""{btnCss}""
   onclick=""_my_event('send','event','btnWatchDog','OutOf','authenticated');"" role=""button"">");
                    if (ShowButtonIcon)
                        sb.Append(@"<span class=""fas fa-eye dark""></span>");
                    sb.Append($@"<span class=""btn-emphasized"">{btnText}</span></a>");
                }

                if (ShowWDList)
                {
                    sb.Append($@"
<a href=""/manage/Watchdogs"" class=""{btnCss}"" style=""padding-left:15px;padding-right:15px""
   alt=""Všichni uložení hlídači"" title=""Všichni uložení hlídači""
   onclick=""_my_event('send','event','btnWatchDog','List','authenticated');"" role=""button"">");
                    if (ShowButtonIcon)
                        sb.Append(@"<span class=""fas fa-eye dark""></span>&nbsp;<span class=""fas fa-list""></span>");
                    else
                        sb.Append(@"<span>Všichni uložení hlídači</span>");
                    sb.Append("</a>");
                }

                sb.Append(postText);

                // --- Modal (authenticated) ---
                sb.Append($@"
<div class=""modal fade"" id=""addWDform{uid}"" tabindex=""-1"" role=""dialog"">
  <div class=""modal-dialog"" role=""document"">
    <div class=""modal-content"">
      <div class=""modal-header"">
        <h4 class=""modal-title"">Přidání nového hlídače</h4>
        <button type=""button"" onclick=""_my_event('send','event','btnWatchDog','CloseBtnIcon','authenticated');""
                class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
      </div>
      <div class=""modal-body text-start"">
        <div>
          <div class=""col-xs-12"">
            <label for=""wdname{uid}"" class=""control-label"">Pojmenování hlídače</label>
            <div>
              <input type=""text"" class=""form-control--small"" name=""wdname{uid}"" id=""wdname{uid}""
                     placeholder=""Pojmenování hlídače pro přehlednost"" value=""{HtmlEscape(PrefillWDname)}"">
            </div>
          </div>
          <div class=""col-xs-12"">
            <label for=""period{uid}"" class=""control-label"">Jak často máme kontrolovat nové {dataTypeName}?</label>
            <div>
              <select class=""form-control--small"" name=""period{uid}"" id=""period{uid}"">");

                foreach (var kv in periodList)
                {
                    var selected = kv.Value == "2" ? @" selected=""selected""" : "";
                    var disabled = kv.Disabled ? @" disabled=""disabled""" : "";
                    sb.Append($@"<option value=""{kv.Value}""{selected}{disabled}>{HtmlEscape(kv.Text)}</option>");
                }

                sb.Append(@"</select>
            </div>
          </div>");

                // focus select dle typu
                if (Datatype == typeof(Smlouva))
                {
                    sb.Append($@"
          <div class=""col-xs-12"">
            <label for=""focus{uid}"" class=""control-label"">Typ upozornění</label>
            <div>
              <select class=""form-control--small"" name=""focus{uid}"" id=""focus{uid}"">
                <option value=""0"">Zašlete mi přehled o nových smlouvách v Registru smluv</option>
                <option value=""1"">Upozorněte mě na problémy v nových smlouvách</option>
              </select>
            </div>
          </div>");
                }
                else if (Datatype == typeof(VerejnaZakazka))
                {
                    sb.Append($@"
          <div class=""col-xs-12"">
            <label for=""focus{uid}"" class=""control-label"">Typ veřejných zakázek</label>
            <div>
              <select class=""form-control--small"" name=""focus{uid}"" id=""focus{uid}"">
                <option value=""0"">Zašlete mi info o jakékoliv změně veřejné zakázky</option>
                <option value=""1"">Upozorněte mě pouze na zakázky do kterých se dá ještě přihlásit</option>
              </select>
            </div>
          </div>");
                }
                else
                {
                    sb.Append($@"<input type=""hidden"" name=""focus{uid}"" id=""focus{uid}"" value=""0"">");
                }

                sb.Append($@"
        </div>
      </div>
      <div class=""modal-footer"">
        <button type=""button"" class=""btn btn-hs btn-secondary"" data-bs-dismiss=""modal"">Zavřít</button>
        <a href=""#"" role=""button"" class=""btn btn-hs btn-success""
           onclick=""javascript: _my_event('send','event','btnWatchDog','AddNew','authenticated');AddNewWD('{JsEscape(Query)}','{datatypeApi}',$('#wdname{uid}').val(),$('#period{uid}').val(), $('#focus{uid}').val(), this);return false;"">
           Přidat hlídače
        </a>
      </div>
    </div>
  </div>
</div>");
            }
            else
            {
                // --- Anonymní ---
                sb.Append($@"
<a href=""#"" onclick=""_my_event('send','event','btnWatchDog','AddNew','anonym');""
   class=""{btnCss}"" role=""button"" data-bs-toggle=""modal"" data-bs-target=""#addWDhelp{uid}"">");
                if (ShowButtonIcon)
                    sb.Append(@"<span class=""fas fa-eye dark""></span>");
                sb.Append($@"<span class=""btn-emphasized"">{btnText}</span></a>");

                sb.Append(postText);

                sb.Append($@"
<div class=""modal fade"" id=""addWDhelp{uid}"" tabindex=""-1"" role=""dialog"">
  <div class=""modal-dialog"" role=""document"">
    <div class=""modal-content"">
      <div class=""modal-header"">
        <h4 class=""modal-title"">Musíte být přihlášeni</h4>
        <button type=""button"" onclick=""_my_event('send','event','btnWatchDog','CloseBtnIcon','authenticated');""
                class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
      </div>
      <div class=""modal-body"">
        <p><code>Hlídač nových smluv</code> posílá upozornění pouze zaregistrovaným uživatelům.</p>
        <p>Registrace je jednoduchá a zdarma. Pokud už jste zaregistrováni, stačí se přihlásit.</p>
      </div>
      <div class=""modal-footer"">
        <button type=""button"" onclick=""_my_event('send','event','btnWatchDog','CloseForm','anonym');""
                class=""btn btn-hs btn-secondary"" data-bs-dismiss=""modal"">Zavřít</button>
        <a onclick=""_my_event('send','event','registerBtn','click','/@this.Path'); return true;""
           href=""/account/register?retUrl_2={retUrl_2}""
           role=""button"" class=""btn btn-hs btn-primary"">Zdarma zaregistrovat</a>
        <a onclick=""_my_event('send','event','registerBtn','click','/@this.Path'); return true;""
           href=""/account/login?retUrl_2={retUrl_2}""
           role=""button"" class=""btn btn-hs btn-success"">Přihlásit se</a>
      </div>
    </div>
  </div>
</div>");
            }

            output.Content.SetHtmlContent(sb.ToString());
            await Task.CompletedTask;
        }

        private static (string one, string two) GetTypeNames(Type? t)
        {
            string one = "smlouvy";
            string two = "smluv";
            if (t == typeof(VerejnaZakazka)) { one = "zakázky"; two = "zakázek"; }
            else if (t == typeof(Rizeni)) { one = "insolvence"; two = "insolvencí"; }
            else if (t == typeof(DataSet)) { one = "záznamy v databázi"; two = "záznamů v databázích"; }
            else if (t == null) { one = "informace na Hlídači"; two = "informací v celém Hlídači"; }
            return (one, two);
        }

        private static string HtmlEscape(string? s)
            => string.IsNullOrEmpty(s) ? "" : System.Net.WebUtility.HtmlEncode(s);

        private static string JsEscape(string? s)
            => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("'", "\\'");
    }
}