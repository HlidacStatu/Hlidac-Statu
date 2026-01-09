using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework
{
    public static class WebUtil
    {
        public static IHtmlContent IfExists(bool showExists, string exists, string ifEmpty = "")
        {
            if (showExists)
            {
                return new HtmlString(exists);
            }
            else
            {
                return new HtmlString(ifEmpty);
            }
        }

        public static string GetSearchUrl(string url, string? query)
        {
            if (query != null)
            {
                if (url.Contains("?"))
                {
                    url = url + "&q=" + System.Net.WebUtility.UrlEncode(query);
                }
                else
                {
                    url = url + "?q=" + System.Net.WebUtility.UrlEncode(query);
                }
            }

            return url;
        }

        public static IHtmlContent AddSearchBtn(string url, string? query = null, string btnText = "Hledat",
            string? btnCss = null)
        {
            return new HtmlString(
                $@"<a class=""{btnCss ?? "btn btn-secondary btn-xs"}"" href=""{GetSearchUrl(url, query)}"">
                <span class=""fad fa-search dark"" aria-hidden=""true""></span>{btnText}</a>");
        }

        public static IHtmlContent SetAutofocusOnId(string id)
        {
            return new HtmlString(
                $@"<script>
window.onload = function() {{ 
    var input = document.getElementById(""{id}"");
    if (input)
        input.focus();
    }}
</script>");
        }

        public static IHtmlContent AddVisitImg(string path)
        {
            string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(path));
            return new HtmlString($"<text><img src=\"/visitimg/{base64}\" width=\"1\" height=\"1\" /></text>");
        }

        public static IHtmlContent NoWrapHtml(params IHtmlContent[] parts)
        {
            var s = string.Join("", parts.Select(m => m.ToString()?.Replace("\n", "").Trim()));
            return new HtmlString($"<span style='white-space:nowrap'>{s}</span>");
        }

        public static async Task<IHtmlContent> RenderOsobaVazbaAsync(HlidacStatu.DS.Graphs.Graph.Edge v,
            string blockFormat = "<div>{0}</div>")
        {
            var sb = new StringBuilder();

            var fname = $"<a href='{Firma.GetUrl(v.To.Id, true)}'>{v.To.PrintNameAsync()}</a>";
            
            if (string.IsNullOrEmpty(await v.To.PrintNameAsync()))
            {
                fname = $"<a href='{Firma.GetUrl(v.To.Id, true)}'>{FirmaRepo.NameFromIco(v.To.Id, true)}</a>";
            }

            sb.Append(fname).Append(" - ").Append(v.Descr).Append("&nbsp;");

            if (v.RelFrom.HasValue && v.RelTo.HasValue)
            {
                sb.Append($"({v.RelFrom.Value.ToShortDateString()} - {v.RelTo.Value.ToShortDateString()})");
            }
            else if (v.RelTo.HasValue)
            {
                sb.Append($"(do {v.RelTo.Value.ToShortDateString()})");
            }
            else if (v.RelFrom.HasValue)
            {
                sb.Append($"(od {v.RelFrom.Value.ToShortDateString()})");
            }

            return new HtmlString(string.Format(blockFormat, sb.ToString()));
        }

        public static string GenerateCacheKey(object[] objects)
        {
            return string.Join("_", objects);
        }

        public static string RenderProperty(JToken jp, int level, int maxLevel, int? maxLength = null)
        {
            if (jp == null)
                return string.Empty;
            if (level > maxLevel)
                return string.Empty;

            switch (jp.Type)
            {
                case JTokenType.None:
                    return string.Empty;
                case JTokenType.Object:
                    if (level < maxLevel)
                        return RenderObject(jp, level + 1, maxLevel, maxLength);
                    break;
                case JTokenType.Array:
                    var vals = jp.Values<JToken>();
                    if (vals != null && vals.Count() > 0)
                    {
                        return string.Join("\n", vals.Select(v => RenderProperty(v, level, maxLevel, maxLength)));
                    }

                    break;
                case JTokenType.Constructor:
                    //
                    break;
                case JTokenType.Property:
                    return RenderProperty(jp.Value<JProperty>().Children().FirstOrDefault(), level, maxLevel,
                        maxLength);
                case JTokenType.Comment:
                    break;
                case JTokenType.Integer:
                    return jp.Value<int>().ToString();
                case JTokenType.Float:
                    return jp.Value<float>().ToString(HlidacStatu.Util.Consts.czCulture);
                case JTokenType.String:
                    return ShortenText(jp.Value<string>(), maxLength);
                case JTokenType.Boolean:
                    break;
                case JTokenType.Null:
                    break;
                case JTokenType.Undefined:
                    break;
                case JTokenType.Date:
                    return jp.Value<DateTime>().ToString("d.M.yyyy");
                case JTokenType.Raw:
                    return ShortenText(jp.Value<string>().ToString(), maxLength);
                case JTokenType.Bytes:
                    break;
                case JTokenType.Guid:
                    return jp.Value<Guid>().ToString();
                case JTokenType.Uri:
                    return ShortenText(jp.Value<Uri>().ToString(), maxLength);
                case JTokenType.TimeSpan:
                    break;
                default:
                    break;
            }

            return string.Empty;
        }

        public static string RenderObject(JToken jo, int level, int maxLevel, int? maxLength = null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("(");
            foreach (JProperty jtoken in jo.Children())
            {
                sb.Append(string.Format("{0}:{1}, ", jtoken.Name, RenderProperty(jtoken, level, maxLevel, maxLength)));
            }

            if (sb.Length > 3)
                sb.Remove(sb.Length - 3, 2); //remove last ,_
            sb.Append(")");
            return sb.ToString();
        }

        public static string ShortenText(dynamic value, int? length = null)
        {
            if (value == null)
                return string.Empty;
            else
            {
                if (length.HasValue == false)
                    return value.ToString();
                return Devmasters.TextUtil.ShortenText(value.ToString(), length.Value);
            }
        }
    }
}