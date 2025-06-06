﻿using Google.Protobuf.WellKnownTypes;
using HlidacStatu.Datasets;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories;

using Newtonsoft.Json.Linq;

using Scriban.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.XLib.Render
{
    public partial class ScribanT
    {
        public partial class Functions : ScriptObject
        {
            public static string fn_LinkTextDocument(dynamic value, string datasetId, string dataId, string linkText = "")
            {
                return fn_LinkTextDocumentWithHighlighting(value, datasetId, dataId, linkText);
            }
            public static string fn_LinkTextDocumentWithHighlighting(
                dynamic value, string datasetId, string dataId, string linkText = "",
                IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlightingData = null, string highlPrefix = "", string highlPostfix = "")
            {

                if (value == null)
                    return string.Empty;
                else
                {
                    if (string.IsNullOrEmpty(linkText))
                        linkText = "Textový obsah dokumentu";
                    var jobj = value as JObject;
                    if (jobj != null)
                    {
                        if (
                            DataSet.OCRCommands.Contains(jobj["HsProcessType"].Value<string>())
                            && Uri.TryCreate(jobj["DocumentUrl"].Value<string>(), UriKind.Absolute, out var uri)
                            && !string.IsNullOrEmpty(jobj["DocumentPlainText"].Value<string>())
                            )
                        {
                            string HLresult = null;
                            if (highlightingData != null)
                            {
                                string path = System.Text.RegularExpressions.
                                    Regex.Replace(jobj.Path, @"\[\d{1,}\]", "")
                                    + ".DocumentPlainText";
                                HLresult = HlidacStatu.Searching.Highlighter.HighlightContentIntoHtmlBlock(highlightingData,
                                    path,
                                    jobj["DocumentPlainText"].Value<string>(), " ..... ",
                                    prefix: highlPrefix, postfix: highlPostfix);
                            }

                            string result = $"<a href=\"https://www.hlidacstatu.cz/data/DetailText/{datasetId}/{dataId}?p={jobj.Path}\"><b>{linkText}</b></a> (zde <a target=\"_blank\" href=\"{uri.AbsoluteUri}\">originál ke stažení</a>)";
                            if (!string.IsNullOrEmpty(HLresult))
                                result = result + "<br/>" + HLresult;
                            return result;
                        }
                        else if (
                            DataSet.OCRCommands.Contains(jobj["HsProcessType"].Value<string>())
                            && Uri.TryCreate(jobj["DocumentUrl"].Value<string>(), UriKind.Absolute, out var uri2)
                            )
                        {
                            return $"<a href=\"{uri2.AbsoluteUri}\">{linkText}</a>";
                        }
                    }

                    return "";
                }
            }

            public static string fn_RenderPersonNoLink(string osobaId, string jmeno = "", string prijmeni = "", string rokNarozeni = "")
            {
                if (!string.IsNullOrEmpty(osobaId))
                {
                    Osoba o = Osoby.GetByNameId.Get(osobaId);
                    if (o != null)
                        return $"<span>{o.FullNameWithYear(false)}</span>";
                }

                var narozeni = "";
                if (!string.IsNullOrEmpty(rokNarozeni))
                    narozeni = $"(* {rokNarozeni})";
                if (!string.IsNullOrEmpty(jmeno) || !string.IsNullOrEmpty(prijmeni))
                    return $"<span>{jmeno} {prijmeni} {narozeni}</span>";
                else
                    return "";
            }

            public static string fn_RenderPersonWithLink(string osobaId, string jmeno, string prijmeni, string rokNarozeni = "")
            {
                if (!string.IsNullOrEmpty(osobaId))
                {
                    Osoba o = Osoby.GetByNameId.Get(osobaId);
                    if (o != null)
                        return $"<span><a href=\"{o.GetUrl(false)}\">{o.FullNameWithYear(false)}</a></span>";
                }

                var narozeni = "";
                if (!string.IsNullOrEmpty(rokNarozeni))
                    narozeni = $"(* {rokNarozeni})";
                return $"<span>{jmeno} {prijmeni} {narozeni}</span>";
            }

            public static string fn_RenderPersonStatistic(string osobaId, bool twoLines = false, string prefix = "", string postfix = "")
            {
                if (!string.IsNullOrEmpty(osobaId))
                {
                    Osoba o = Osoby.GetByNameId.Get(osobaId);
                    if (o != null)
                    {
                        var stat = o.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny);
                        //return $"<span>{prefix}{stat.BasicStatPerYear.SummaryAfter2016().ToNiceString(o, true, customUrl: "/hledatSmlouvy?q=osobaId:" + o.NameId, twoLines: twoLines)}{postfix}</span>";
                        var s = stat.SoukromeFirmy.Values
                                        .AggregateStats()
                                        .Summary(CoreStat.UsualYearsInterval.FromUsualFirstYearUntilSeassonYear)
                                        .ToNiceString(o, true, customUrl: "/hledatSmlouvy?q=osobaId:" + o.NameId, twoLines: twoLines);

                        return $"<span>{prefix}{s}{postfix}</span>";
                    }
                }
                return string.Empty;
            }

            public static string fn_RenderPersonWithLink2(string osobaId)
            {
                if (!string.IsNullOrEmpty(osobaId))
                {
                    Osoba o = Osoby.GetByNameId.Get(osobaId);
                    if (o != null)
                        return $"<span><a href=\"{o.GetUrl(false)}\">{o.FullNameWithYear(false)}</a></span>";
                }
                return string.Empty;
            }

            public static string fn_RenderCompanyName(string ico, string missingCompanyName = "")
            {
                ico = HlidacStatu.Util.ParseTools.NormalizeIco(ico);
                if (!string.IsNullOrEmpty(ico))
                {
                    Firma o = Firmy.instanceByIco.Get(ico);
                    if (o?.Valid == true)
                    {
                        return $"<span>{o.Jmeno}</span>";
                    }
                    else
                        return $"<span>{(string.IsNullOrEmpty(missingCompanyName) ? ico : missingCompanyName)}</span>";
                }
                return string.Empty;
            }

            public static string fn_RenderCompanyWithLink(string ico, string missingCompanyName = "")
            {
                if (ico == null)
                    return string.Empty;

                ico = HlidacStatu.Util.ParseTools.NormalizeIco(ico);
                if (!string.IsNullOrEmpty(ico))
                {
                    Firma o = Firmy.instanceByIco.Get(ico);
                    if (o?.Valid == true)
                    {
                        //var lbl = Lib.Analysis.KorupcniRiziko.KIndex.GetLastLabel(ico);
                        //string skidx = "";
                        //if (lbl != null && lbl.Item2 != Lib.Analysis.KorupcniRiziko.KIndexData.KIndexLabelValues.None)
                        //{
                        //    skidx = $"<a href='/kindex/detail/{ico}'>"
                        //        + $"<img src='{Lib.Analysis.KorupcniRiziko.KIndexData.KIndexLabelIconUrl(lbl.Item2)}' class='kindex' style='padding:0 3px;height:15px;width:auto' />"
                        //        + "</a>";
                        //}

                        return $"<span>"
                            //+ skidx //TODO KIDX
                            + $"<a href=\"{o.GetUrl(false)}\">{o.Jmeno}</a></span>";
                    }
                    else
                        return $"<span>{(string.IsNullOrEmpty(missingCompanyName) ? ico : missingCompanyName)}</span>";
                }
                return string.Empty;
            }

            public static string fn_RenderCompanyStatistic(string ico, bool twoLines = false, string prefix = "", string postfix = "")
            {
                if (ico  == null)
                    return string.Empty;
                ico = HlidacStatu.Util.ParseTools.NormalizeIco(ico);
                if (!string.IsNullOrEmpty(ico))
                {
                    var firma = Firmy.instanceByIco.Get(ico);
                    if (firma.Valid)
                    {
                        var stat = firma.StatistikaRegistruSmluv();
                        var pocet = stat.Sum(stat.YearsAfter2016(), s => s.PocetSmluv);
                        var celkem = stat.Sum(stat.YearsAfter2016(), s => s.CelkovaHodnotaSmluv);

                        string niceString = $"<a href='/hledatSmlouvy?q=ico:{firma.ICO}'>" +
                            Devmasters.Lang.CS.Plural.Get(pocet, "{0} smlouva;{0} smlouvy;{0} smluv") +
                            "</a>" + (twoLines ? "<br />" : " za ") +
                            "celkem " + Smlouva.NicePrice(celkem, html: true, shortFormat: true);

                        return $"<span>{prefix}{niceString}{postfix}</span>";
                    }
                    else
                        return $"";
                }
                return string.Empty;
            }
            public static string fn_RenderCompanyInformations(string ico, int numberOfInfos = 3, string prefix = "", string postfix = "",
                string delimiterBetweenInfos = "")
            {
                if (ico == null)
                    return string.Empty;

                ico = HlidacStatu.Util.ParseTools.NormalizeIco(ico);
                if (!string.IsNullOrEmpty(ico))
                {
                    var o = Firmy.instanceByIco.Get(ico);
                    if (o?.Valid == true)
                    {
                        string niceString = o.InfoFacts().RenderFacts(numberOfInfos, true, false, delimiterBetweenInfos);

                        return $"<span>{prefix}{niceString}{postfix}</span>";
                    }
                    else
                        return string.Empty;
                }
                return string.Empty;
            }
            public static string fn_LastChars(dynamic value, int? length = null)
            {

                if (value == null)
                    return string.Empty;
                else
                {
                    if (length.HasValue == false)
                        return value.ToString();
                    if (length == 0)
                        return string.Empty;

                    string s = value.ToString();
                    if (s.Length <= length)
                        return s;
                    else
                        return s.Substring((s.Length - length.Value), length.Value);
                }
            }

            public static string fn_ShortenText(dynamic value, int? length = null)
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

            public static string fn_FormatDurationInSec(dynamic value, string format = null)
            {
                if (value == null)
                    return string.Empty;

                int? sec = Util.ParseTools.ToInt(value.ToString());
                if (sec.HasValue)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(sec.Value);
                    format = format ?? "c";
                    return ts.ToString(format);
                }
                else
                {
                    return value.ToString();
                }
            }

            public static string fn_FormatNumber(dynamic value, string format = null)
            {
                if (value == null)
                    return string.Empty;

                format = format ?? "cs";
                decimal? dat = Util.ParseTools.ToDecimal(value.ToString());
                if (dat.HasValue)
                {
                    if (format == "en")
                    {
                        return dat.Value.ToString(Util.Consts.enCulture);
                    }
                    else
                    {
                        return dat.Value.ToString(Util.Consts.czCulture);
                    }
                }
                else
                {
                    return value.ToString();
                }
            }


            public static string fn_FormatDate(dynamic value, string format = null)
            {
                return fn_FormatDate2(value, format,
                        "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ss.ffffK", "yyyy-MM-ddTHH:mm:ss.fffffK",
                        "yyyy-MM-dd HH:mm:ss",
                        "yyyy-MM-dd HH:mm:ss.fK", "yyyy-MM-dd HH:mm:ss.ffK", "yyyy-MM-dd HH:mm:ss.fffK", "yyyy-MM-dd HH:mm:ss.ffffK",
                        "yyyy-MM-dd HH:mm:ss.f", "yyyy-MM-dd HH:mm:ss.ff", "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss.ffff",
                        "dd.MM.yyyy HH: mm:ss", "d.M.yyyy H:m:s", "dd.MM.yyyy", "d.M.yyyy",
                        "yyyy.MM.dd HH: mm:ss", "yyyy.M.d H:m:s", "yyyy.MM.dd", "yyyy.M.d",
                        "yy.MM.dd HH: mm:ss", "yy.M.d H:m:s", "yy.MM.dd", "yy.M.d",
                        "yyyy-MM-dd HH: mm:ss", "yyyy-M-d H:m:s", "yyyy-MM-dd", "yyyy-M-d",
                        "yy-MM-dd HH: mm:ss", "yy-M-d H:m:s", "yy-MM-dd", "yy-M-d"
                    );
            }
            public static string fn_FormatDate2(dynamic value, string format = null, params string[] inputformats)
            {
                if (value == null)
                    return "";

                if (inputformats == null)
                {
                    inputformats = new string[] { };
                }
                format = format ?? "d.MM.yyyy";
                DateTime? dat = Devmasters.DT.Util.ToDateTime(value.ToString(), inputformats);
                if (dat.HasValue)
                    return dat.Value.ToString(format);
                else
                    return value.ToString();
            }

            public static string fn_FormatPrice(dynamic value, string mena = null, bool html = true, bool shortFormat = false)
            {
                if (value == null)
                    return string.Empty;

                mena = mena ?? "Kč";
                decimal? val = Util.ParseTools.ToDecimal(value.ToString());
                if (val.HasValue)
                {
                    return Util.RenderData.NicePrice(val.Value, mena: mena, html: html, shortFormat: shortFormat);
                }
                return "";
            }


            public static string fn_FixPlainText(dynamic text)
            {
                if (text == null)
                    return string.Empty;
                var s = text.ToString();

                //remove /n/r on the beginning
                s = System.Text.RegularExpressions.Regex.Replace(s, "^(\\s)*", "");
                s = Devmasters.TextUtil.ReplaceDuplicates(s, "\n\n");
                s = Devmasters.TextUtil.ReplaceDuplicates(s, "\r\r");
                s = Devmasters.TextUtil.ReplaceDuplicates(s, "\t\t");

                return s;
                //return s;
            }

            public static string fn_HighlightText(IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlightingData, dynamic text, string attrPath)
            {
                var s = fn_FixPlainText(text);



                return "<div class='highlighting'>"
                    + Searching.Highlighter.HighlightFullContent(highlightingData, attrPath, s)
                    + "</div>";
                //return s;
            }

            public static string fn_NormalizeText(dynamic text)
            {
                if (text == null)
                    return string.Empty;
                else
                    return Devmasters.TextUtil.ReplaceHTMLEntities(text.ToString());
            }

            public static string fn_Pluralize(int number, string zeroText, string oneText, string twoText, string moreText)
            {
                return Devmasters.Lang.CS.Plural.GetWithZero(number, zeroText, oneText, twoText, moreText);
            }

            public static string fn_GetRegexGroupValue(string text, string regex, string groupname)
            {
                return Devmasters.RegexUtil.GetRegexGroupValue(text, regex, groupname);
            }

            public static bool fn_IsNullOrEmpty(dynamic text)
            {
                if (text == null)
                {
                    return true;
                }
                try
                {
                    string s = (string)text;
                    return string.IsNullOrEmpty(s);

                }
                catch (Exception)
                {
                    return false;
                }

            }


            public static string xfn_RenderObject(JToken jo, int level, int maxLevel, int? maxLength = null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("(");
                foreach (JProperty jtoken in jo.Children())
                {
                    sb.Append(string.Format("{0}:{1}, ", jtoken.Name, xfn_RenderProperty(jtoken, level, maxLevel, maxLength)));
                }
                if (sb.Length > 3)
                    sb.Remove(sb.Length - 3, 2); //remove last ,_
                sb.Append(")");
                return sb.ToString();
            }

            public static string xfn_RenderProperty(JToken jp, int level, int maxLevel, int? maxLength = null)
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
                            return xfn_RenderObject(jp, level + 1, maxLevel, maxLength);
                        break;
                    case JTokenType.Array:
                        var vals = jp.Values<JToken>();
                        if (vals != null && vals.Count() > 0)
                        {
                            return vals.Select(v => xfn_RenderProperty(v, level, maxLevel, maxLength)).Aggregate((f, s) => f + "\n" + s);
                        }
                        break;
                    case JTokenType.Constructor:
                        //
                        break;
                    case JTokenType.Property:
                        return xfn_RenderProperty(jp.Value<JProperty>().Children().FirstOrDefault(), level, maxLevel, maxLength);
                    case JTokenType.Comment:
                        break;
                    case JTokenType.Integer:
                        return jp.Value<int>().ToString();
                    case JTokenType.Float:
                        return jp.Value<float>().ToString(Util.Consts.czCulture);
                    case JTokenType.String:
                        return fn_ShortenText(jp.Value<string>(), maxLength);
                    case JTokenType.Boolean:
                        break;
                    case JTokenType.Null:
                        break;
                    case JTokenType.Undefined:
                        break;
                    case JTokenType.Date:
                        return jp.Value<DateTime>().ToString("d.M.yyyy");
                    case JTokenType.Raw:
                        return fn_ShortenText(jp.Value<string>().ToString(), maxLength);
                    case JTokenType.Bytes:
                        break;
                    case JTokenType.Guid:
                        return jp.Value<Guid>().ToString();
                    case JTokenType.Uri:
                        return fn_ShortenText(jp.Value<Uri>().ToString(), maxLength);
                    case JTokenType.TimeSpan:
                        break;
                    default:
                        break;
                }

                return string.Empty;

            }
        }

    }
}
