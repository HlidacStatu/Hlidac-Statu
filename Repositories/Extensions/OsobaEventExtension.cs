using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Text;

namespace HlidacStatu.Extensions
{
    public static class OsobaEventExtension
    {
        public static string RenderText(this OsobaEvent osobaEvent, string delimeter = "\n")
        {
            StringBuilder sb = new StringBuilder();
            switch ((OsobaEvent.Types)osobaEvent.Type)
            {

                case OsobaEvent.Types.Politicka:
                case OsobaEvent.Types.PolitickaExekutivni:
                case OsobaEvent.Types.VerejnaSpravaJine:
                case OsobaEvent.Types.VerejnaSpravaExekutivni:
                case OsobaEvent.Types.SoukromaPracovni:
                case OsobaEvent.Types.VolenaFunkce:
                    sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                    if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                        sb.Append(" - " + osobaEvent.Organizace);
                    return sb.ToString();
                case OsobaEvent.Types.Osobni:
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo) && Devmasters.TextUtil.IsNumeric(osobaEvent.AddInfo))
                    {
                        Osoba o = Osoby.GetById.Get(Convert.ToInt32(osobaEvent.AddInfo));
                        if (o != null)
                            return osobaEvent.Title + " s " + o.FullName();
                    }
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo + osobaEvent.Organizace))
                    {
                        sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                        if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                            sb.Append(" - " + osobaEvent.Organizace);
                        return sb.ToString();
                    }
                    else
                        return (osobaEvent.Title + " " + osobaEvent.Note).Trim();

                case OsobaEvent.Types.Vazby:
                default:
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo + osobaEvent.Organizace))
                    {
                        sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                        if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                            sb.Append(" - " + osobaEvent.Organizace);
                        return sb.ToString();
                    }
                    if (!string.IsNullOrEmpty(osobaEvent.Title) && !string.IsNullOrEmpty(osobaEvent.Note))
                        return osobaEvent.Title + delimeter + osobaEvent.Note;
                    else if (!string.IsNullOrEmpty(osobaEvent.Title))
                        return osobaEvent.Title;
                    else if (!string.IsNullOrEmpty(osobaEvent.Note))
                        return osobaEvent.Note;
                    else
                        return string.Empty;
            }
        }

        public static string RenderHtml(this OsobaEvent osobaEvent, string delimeter = ", ")
        {
            string zdroj = "";
            if (!string.IsNullOrEmpty(osobaEvent.Zdroj))
            {
                if (osobaEvent.Zdroj.ToLower().StartsWith("http"))
                    zdroj = string.Format(" <a target='_blank' href='{0}'>{1}</a>", osobaEvent.Zdroj, "<span class='text-muted' title='Jedná se o peněžní nebo nepeněžní dar' alt='Jedná se o peněžní nebo nepeněžní dar'>(<span class='glyphicon glyphicon-link' aria-hidden='true'></span> zdroj</span>)");
                else
                    zdroj = string.Format(" ({0})", osobaEvent.Zdroj);

            }
            StringBuilder sb = new StringBuilder();
            switch ((OsobaEvent.Types)osobaEvent.Type)
            {
                case OsobaEvent.Types.Politicka:
                case OsobaEvent.Types.PolitickaExekutivni:
                case OsobaEvent.Types.VerejnaSpravaJine:
                case OsobaEvent.Types.VerejnaSpravaExekutivni:
                case OsobaEvent.Types.SoukromaPracovni:
                case OsobaEvent.Types.VolenaFunkce:
                    sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                    if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                        sb.Append(" - " + osobaEvent.Organizace);
                    return sb.ToString();
                case OsobaEvent.Types.Osobni:
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo) && Devmasters.TextUtil.IsNumeric(osobaEvent.AddInfo))
                    {
                        Osoba o = Osoby.GetById.Get(Convert.ToInt32(osobaEvent.AddInfo));
                        if (o != null)
                            return osobaEvent.Title + " s " + string.Format("<a href=\"{0}\">{1}</a>", o.GetUrl(), o.FullName());
                    }
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo + osobaEvent.Organizace))
                    {
                        sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                        if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                            sb.Append(" - " + osobaEvent.Organizace);
                        return sb.ToString();
                    }
                    else
                        return (osobaEvent.Title + " " + osobaEvent.Note).Trim();

                case OsobaEvent.Types.Vazby:
                default:
                    if (!string.IsNullOrEmpty(osobaEvent.AddInfo + osobaEvent.Organizace))
                    {
                        sb.Append($"{osobaEvent.AddInfo} {osobaEvent.RenderDatum(txtOd: "od", txtDo: " do ", template: "({0})")} ");
                        if (!string.IsNullOrEmpty(osobaEvent.Organizace))
                            sb.Append(" - " + osobaEvent.Organizace);
                        return sb.ToString();
                    }
                    if (!string.IsNullOrEmpty(osobaEvent.Title) && !string.IsNullOrEmpty(osobaEvent.Note))
                        return osobaEvent.Title + delimeter + osobaEvent.Note + zdroj;
                    else if (!string.IsNullOrEmpty(osobaEvent.Title))
                        return osobaEvent.Title + zdroj;
                    else if (!string.IsNullOrEmpty(osobaEvent.Note))
                        return osobaEvent.Note + zdroj;
                    else
                        return string.Empty;
            }
        }


        private static string RenderDatum(this OsobaEvent oEvent, string txtOd = "", string txtDo = " - ", string dateFormat = "yyyy", string template = "{0}")
        {
            return RenderDatum(oEvent.DatumOd, oEvent.DatumDo, txtOd, txtDo, dateFormat, template);
        }
        private static string RenderDatum(DateTime? DatumOd, DateTime? DatumDo, string txtOd = "", string txtDo = "–", string dateFormat = "yyyy", string template = "{0}")
        {
            if (DatumOd.HasValue && DatumDo.HasValue)
            {
                //check whole year.
                if (DatumOd.Value.Year == DatumDo.Value.Year
                    && DatumOd.Value.Month == 1 && DatumDo.Value.Month == 12
                    && DatumOd.Value.Day == 1 && DatumDo.Value.Day == 31
                    )
                    return string.Format(template, DatumOd.Value.Year.ToString());
                else
                {
                    return string.Format(template,
                        string.Format("{0} {1}{2}{3}",
                           txtOd,
                           DatumOd.Value.ToString(dateFormat),
                           txtDo,
                           DatumDo.Value.ToString(dateFormat)
                           ).Trim()
                        );
                }
            }
            else if (DatumOd.HasValue)
            {
                return string.Format(template,
                        string.Format("{0} {1}",
                            txtOd,
                            DatumOd.Value.ToString(dateFormat)
                            ).Trim()
                        );
            }
            else if (DatumDo.HasValue)
            {
                return string.Format(template,
                        string.Format("{0} {1}",
                        txtDo,
                        DatumDo.Value.ToString(dateFormat)
                        ).Trim()
                    );
            }
            else
                return string.Empty;
        }
    }
}