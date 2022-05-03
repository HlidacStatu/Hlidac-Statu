using Devmasters.Enums;

using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities.Analysis;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Util;
using HlidacStatu.XLib.Render;

using System;
using System.Collections.Generic;
using System.Net;

using Consts = HlidacStatu.Util.Consts;

namespace HlidacStatu.XLib
{
    public static class ReportUtil
    {

        public static List<ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column> ComplexStatisticDefaultReportColumns<T>(RenderData.MaxScale scale,
            int? minDateYear = null, int? maxDateYear = null, string query = null)
        {
            minDateYear = minDateYear ?? DataPerYear.UsualFirstYear;
            maxDateYear = maxDateYear ?? DateTime.Now.Year;

            var coreColumns = new List<ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column>();

            coreColumns.Add(
                new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                {
                    Id = "Title",
                    Name = "Plátci",
                    HtmlRender = (s) =>
                    {
                        var f = Firmy.Get(s.Key);
                        string html = string.Format("<a href='{0}'>{1}</a>", f.GetUrl(false), f.Jmeno);
                        if (!string.IsNullOrEmpty(query))
                        {
                            html += $" /<span class='small'>ukázat&nbsp;<a href='/hledat?q={WebUtility.UrlEncode(Query.ModifyQueryAND("ico:" + f.ICO, query))}'>smlouvy</a></span>/";
                        }
                        return html;
                    },
                    OrderValueRender = (s) => Firmy.GetJmeno(s.Key),
                    ValueRender = (s) => ("\"" + Firmy.GetJmeno(s.Key) + "\""),
                    TextRender = (s) => Firmy.GetJmeno(s.Key)
                });

            for (int y = minDateYear.Value; y <= maxDateYear.Value; y++)
            {
                int year = y;
                coreColumns.Add(
                    new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                    {
                        Id = "Cena_Y_" + year,
                        Name = $"Smlouvy {year} v {scale.ToNiceDisplayName()} Kč",
                        HtmlRender = (s) => RenderData.ShortNicePrice(s.Value[year].CelkovaHodnotaSmluv, mena: "", html: true, showDecimal: RenderData.ShowDecimalVal.Show, exactScale: scale, hideSuffix: true),
                        OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value[year].CelkovaHodnotaSmluv),
                        TextRender = (s) => RenderData.ShortNicePrice(s.Value[year].CelkovaHodnotaSmluv, mena: "", html: false, showDecimal: RenderData.ShowDecimalVal.Show, exactScale: scale, hideSuffix: true),
                        ValueRender = (s) => s.Value[year].CelkovaHodnotaSmluv.ToString("F0", HlidacStatu.Util.Consts.enCulture),
                        CssClass = "number"
                    }
                    );
                coreColumns.Add(
                    new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                    {
                        Id = "Pocet_Y_" + year,
                        Name = $"Počet smluv v {year} ",
                        HtmlRender = (s) => RenderData.NiceNumber(s.Value[year].PocetSmluv, html: true),
                        OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value[year].PocetSmluv),
                        TextRender = (s) => RenderData.NiceNumber(s.Value[year].PocetSmluv, html: false),
                        ValueRender = (s) => s.Value[year].PocetSmluv.ToString("F0", HlidacStatu.Util.Consts.enCulture),
                        CssClass = "number"
                    }
                    );
                coreColumns.Add(
                        new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                        {
                            Id = "PercentBezCeny_Y_" + year,
                            Name = $"Smluv bez ceny za {year} v %",
                            HtmlRender = (s) => s.Value[year].PercentSmluvBezCeny.ToString("P2"),
                            OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value[year].PercentSmluvBezCeny),
                            ValueRender = (s) => (s.Value[year].PercentSmluvBezCeny * 100).ToString(Consts.enCulture),
                            TextRender = (s) => s.Value[year].PercentSmluvBezCeny.ToString("P2"),
                            CssClass = "number"
                        });
                coreColumns.Add(
                        new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                        {
                            Id = "PercentSPolitiky_Y_" + year,
                            Name = $"% smluv s politiky v {year} ",
                            HtmlRender = (s) => s.Value[year].PercentSmluvPolitiky.ToString("P2"),
                            OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value[year].PercentSmluvPolitiky),
                            ValueRender = (s) => (s.Value[year].PercentSmluvPolitiky * 100).ToString(Consts.enCulture),
                            TextRender = (s) => s.Value[year].PercentSmluvPolitiky.ToString("P2"),
                            CssClass = "number"
                        });
                coreColumns.Add(
                        new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                        {
                            Id = "SumKcSPolitiky_Y_" + year,
                            Name = $"Hodnota smluv s politiky za {year}",
                            HtmlRender = (s) => HlidacStatu.Util.RenderData.NicePrice(s.Value[year].SumKcSmluvSponzorujiciFirmy),
                            OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value[year].SumKcSmluvSponzorujiciFirmy),
                            ValueRender = (s) => (s.Value[year].SumKcSmluvSponzorujiciFirmy).ToString("F0", Consts.enCulture),
                            TextRender = (s) => s.Value[year].SumKcSmluvSponzorujiciFirmy.ToString("F0", HlidacStatu.Util.Consts.enCulture),
                            CssClass = "number"
                        });

                if (year > minDateYear)
                {
                    coreColumns.Add(
                            new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                            {
                                Id = "CenaChangePercent_Y_" + year,
                                Name = $"Změna hodnoty smlouvy {year - 1}-{year}",
                                HtmlRender = (s) => s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.HasValue ?
                                    RenderData.ChangeValueSymbol(s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.Value, true) :
                                    "",
                                OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage ?? 0),
                                ValueRender = (s) => s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.HasValue ?
                                    (s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.Value * 100).ToString(Consts.enCulture) :
                                    "",
                                TextRender = (s) => s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.HasValue ?
                                    (s.Value.ChangeBetweenYears(year, m => m.CelkovaHodnotaSmluv).percentage.Value).ToString("P2") :
                                    "",
                                CssClass = "number"
                            }
                        );
                }
            };
            coreColumns.Add(
                new ReportDataSource<KeyValuePair<string, StatisticsPerYear<Smlouva.Statistics.Data>>>.Column()
                {
                    Id = "CenaCelkem",
                    Name = $"Smlouvy 2016-{DateTime.Now.Year} v {scale.ToNiceDisplayName()} Kč",
                    HtmlRender = (s) => RenderData.ShortNicePrice(s.Value.Sum(m => m.CelkovaHodnotaSmluv), mena: "", html: true, showDecimal: RenderData.ShowDecimalVal.Show, exactScale: scale, hideSuffix: true),
                    ValueRender = (s) => (s.Value.Sum(m => m.CelkovaHodnotaSmluv)).ToString("F0", Consts.enCulture),
                    OrderValueRender = (s) => RenderData.OrderValueFormat(s.Value.Sum(m => m.CelkovaHodnotaSmluv)),
                    CssClass = "number"
                });

            return coreColumns;
        }

    }
}
