using Devmasters.Enums;
using HlidacStatu.DS.Api.Firmy;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Repositories;
using Serilog;

namespace HlidacStatu.Extensions;

public static class FirmaExtensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(FirmaExtension));

    public static async Task<SubjektDetailInfo> GetDetailInfoAsync(string ico, string name)
    {
        DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny;

        Firma f = await NajdiFirmuPodleIcoJmenaAsync(ico, name, aktualnost);

        if (f == null || f.Valid == false)
        {
            return null;
        }

        // do work
        HlidacStatu.DS.Api.Firmy.SubjektDetailInfo res = new();

        res.Business_info = await GetFinancialInfoAsync(f.ICO, f.Jmeno);
        if (res.Business_info != null)
        {
            //remove here to avoid duplicated data
            res.Business_info.Source_Url = null;
            res.Business_info.Copyright = null;
            res.Business_info.Jmeno_Firmy = null;
        }

        res.Source_Url = f.GetUrl(false);
        res.Ico = f.ICO;
        res.Jmeno_Firmy = f.Jmeno;
        res.Rizika = (await f.InfoFactsAsync()).RenderFacts(4, true, false);


        res.Kategorie_Organu_Verejne_Moci = (await f.KategorieOVMAsync())
            .Select(m => m.nazev)
            .ToArray();

        if (f.JsemNeziskovka())
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.NeziskovaOrganizace;
        else if (f.JsemOVM())
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.StatniOrgan;
        else if (f.JsemPolitickaStrana())
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.PolitickaStrana;
        else if (f.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatu
                 || f.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatuAlespon25perc
                 || f.JsemStatniFirma())
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.FirmaPatriStatu;
        else if (f.Registrovana_v_zahranici)
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.ZahranicniFirma;
        else if (f.JsemSoukromaFirma())
            res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.SoukromaFirma;


        //KINDEX
        Entities.KIndex.KIndexData kindex = await f.KindexAsync();
        if (kindex != null)
        {
            var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearKIndex;
            for (int i = 0; i <= 3; i++)
            {
                var kidx = kindex.ForYear(maxY - i);
                if (kidx != null)
                {
                    res.KIndex.Add(new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.KIndexData()
                    {
                        KIndex = kidx.KIndexLabel.ToString(),
                        Obrazek_Url = kidx.KIndexLabelIconUrl(false),
                        Popis = kindex.InfoFacts(i).RenderFacts(3, true, true, ", "),
                        Rok = i
                    });
                }
            }
        }

        //Smlouvy
        var smlouvyStat = f.StatistikaRegistruSmluv();
        if (smlouvyStat != null)
        {
            var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearSmlouvy;
            res.Statistika_Registr_Smluv = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
            {
                Rok = 0,
                CelkovaHodnotaSmluv = smlouvyStat.Summary().CelkovaHodnotaSmluv,
                HlavniOblasti = smlouvyStat.Summary().PoOblastech
                    .OrderByDescending(o => o.Value.CelkemCena)
                    .ThenByDescending(o => o.Value.Pocet)
                    .Take(3)
                    .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                    .ToArray(),
                PocetSmluv = smlouvyStat.Summary().PocetSmluv,
                PocetSmluvBezCeny = smlouvyStat.Summary().PocetSmluvBezCeny,
                PocetSmluvSeZasadnimNedostatkem = smlouvyStat.Summary().PocetSmluvSeZasadnimNedostatkem,
                PocetSmluvULimitu = smlouvyStat.Summary().PocetSmluvULimitu
            };

            res.Statistiky_Registr_Smluv_po_Letech = smlouvyStat
                .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                .Select(ss => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                {
                    Rok = ss.Year,
                    CelkovaHodnotaSmluv = ss.Value.CelkovaHodnotaSmluv,
                    HlavniOblasti = ss.Value.PoOblastech
                        .OrderByDescending(o => o.Value.CelkemCena)
                        .ThenByDescending(o => o.Value.Pocet)
                        .Take(3)
                        .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                        .ToArray(),
                    PocetSmluv = ss.Value.PocetSmluv,
                    PocetSmluvBezCeny = ss.Value.PocetSmluvBezCeny,
                    PocetSmluvSeZasadnimNedostatkem = ss.Value.PocetSmluvSeZasadnimNedostatkem,
                    PocetSmluvULimitu = ss.Value.PocetSmluvULimitu,
                    //ZmenaHodnotySmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Hodnota smluv", ss.Value.CelkovaHodnotaSmluv, smlouvyStat.StatisticsForYear(maxY).CelkovaHodnotaSmluv),
                    //ZmenaPoctuSmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Počet smluv", ss.Value.PocetSmluv, smlouvyStat.StatisticsForYear(maxY).PocetSmluv),
                })
                .ToList();
        }


        //dotace
        var dotaceStat = f.StatistikaDotaci();
        if (dotaceStat != null)
        {
            var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearDotace;
            res.Statistika_Dotace = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
            {
                Rok = 0,
                Celkem_Prideleno = dotaceStat.Summary().CelkemPrideleno,
                Pocet_Dotaci = dotaceStat.Summary().PocetDotaci
            };

            res.Statistika_Dotace_po_Letech = dotaceStat
                .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                .Select(ds => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                {
                    Rok = ds.Year,
                    Celkem_Prideleno = ds.Value.CelkemPrideleno,
                    Pocet_Dotaci = ds.Value.PocetDotaci,
                    //ZmenaHodnotyDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Hodnota smluv", ds.Value.CelkemPrideleno, dotaceStat.StatisticsForYear(maxY).CelkemPrideleno),
                    //ZmenaPoctuDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Počet smluv", ds.Value.PocetDotaci, dotaceStat.StatisticsForYear(maxY).PocetDotaci),
                })
                .ToList();
        }

        if (f.Holding(aktualnost)?.Any() == true)
        {
            var smlouvyStatHolding = f.HoldingStatisticsRegistrSmluv();
            var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearSmlouvy;
            if (smlouvyStatHolding != null)
            {
                res.Statistika_Registr_Smluv_pro_Holding = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                {
                    Rok = 0,
                    CelkovaHodnotaSmluv = smlouvyStatHolding.Summary().CelkovaHodnotaSmluv,
                    HlavniOblasti = smlouvyStatHolding.Summary().PoOblastech
                        .OrderByDescending(o => o.Value.CelkemCena)
                        .ThenByDescending(o => o.Value.Pocet)
                        .Take(3)
                        .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                        .ToArray(),
                    PocetSmluv = smlouvyStatHolding.Summary().PocetSmluv,
                    PocetSmluvBezCeny = smlouvyStatHolding.Summary().PocetSmluvBezCeny,
                    PocetSmluvSeZasadnimNedostatkem = smlouvyStatHolding.Summary().PocetSmluvSeZasadnimNedostatkem,
                    PocetSmluvULimitu = smlouvyStatHolding.Summary().PocetSmluvULimitu
                };

                res.Statisticky_Registr_Smluv_pro_Holding_po_Letech = smlouvyStatHolding
                    .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                    .Select(ss => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                    {
                        Rok = ss.Year,
                        CelkovaHodnotaSmluv = ss.Value.CelkovaHodnotaSmluv,
                        HlavniOblasti = ss.Value.PoOblastech
                            .OrderByDescending(o => o.Value.CelkemCena)
                            .ThenByDescending(o => o.Value.Pocet)
                            .Take(3)
                            .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                            .ToArray(),
                        PocetSmluv = ss.Value.PocetSmluv,
                        PocetSmluvBezCeny = ss.Value.PocetSmluvBezCeny,
                        PocetSmluvSeZasadnimNedostatkem = ss.Value.PocetSmluvSeZasadnimNedostatkem,
                        PocetSmluvULimitu = ss.Value.PocetSmluvULimitu,
                        //ZmenaHodnotySmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Hodnota smluv", ss.Value.CelkovaHodnotaSmluv, smlouvyStatHolding.StatisticsForYear(maxY).CelkovaHodnotaSmluv),
                        //ZmenaPoctuSmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Počet smluv", ss.Value.PocetSmluv, smlouvyStatHolding.StatisticsForYear(maxY).PocetSmluv),
                    })
                    .ToList();
            }

            var dotaceStatHolding = f.HoldingStatistikaDotaci();
            maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearDotace;
            if (dotaceStatHolding != null)
            {
                res.Statistika_Dotace_pro_Holding = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                {
                    Rok = 0,
                    Celkem_Prideleno = dotaceStatHolding.Summary().CelkemPrideleno,
                    Pocet_Dotaci = dotaceStatHolding.Summary().PocetDotaci
                };

                res.Statistika_Dotace_pro_Holding_po_Letech = dotaceStatHolding
                    .Where(m => m.Year <= HlidacStatu.Util.Consts.CalculatedCurrentYearDotace &&
                                m.Year >= HlidacStatu.Util.Consts.CalculatedCurrentYearDotace - 3)
                    .Select(ds => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                    {
                        Rok = ds.Year,
                        Celkem_Prideleno = ds.Value.CelkemPrideleno,
                        Pocet_Dotaci = ds.Value.PocetDotaci,
                        //ZmenaHodnotyDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Hodnota smluv", ds.Value.CelkemPrideleno, dotaceStatHolding.StatisticsForYear(maxY).CelkemPrideleno),
                        //ZmenaPoctuDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Počet smluv", ds.Value.PocetDotaci, dotaceStatHolding.StatisticsForYear(maxY).PocetDotaci),
                    })
                    .ToList();
            }
        } //if has holding

        return res;
    }

    public static async Task<SubjektFinancialInfo> GetFinancialInfoAsync(string ico, string name)
    {
        DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny;
        Firma f = await NajdiFirmuPodleIcoJmenaAsync(ico, name, aktualnost);
        if (f == null || f.Valid == false)
        {
            return null;
        }

        HlidacStatu.DS.Api.Firmy.SubjektFinancialInfo res = new();
        res.Source_Url = f.GetUrl(false);
        res.Ico = f.ICO;
        res.Jmeno_Firmy = f.Jmeno;
        res.Omezeni_Cinnosti = string.IsNullOrWhiteSpace(f.StatusFull()) ? null : f.StatusFull();

        res.Kategorie_Organu_Verejne_Moci = null;
        var _kategorie_Organu_Verejne_Moci = (await f.KategorieOVMAsync())
            .Select(m => m.nazev)
            .ToArray();
        if (_kategorie_Organu_Verejne_Moci?.Length > 0)
            res.Kategorie_Organu_Verejne_Moci = _kategorie_Organu_Verejne_Moci;

        res.Pocet_Zamestnancu =
            FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyMagnitude(f.PocetZamKod?.ToString())?.Pretty;
        res.Obrat = FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyTurnover(f.ObratKod?.ToString())?.Pretty;

        res.Obor_Podnikani =
            FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyIndustryToFullName(f.IndustryKod?.ToString(), true, true);
        res.Platce_DPH = f.PlatceDPHKod switch
        {
            1 => "Je plátce DPH",
            0 => "Není plátce DPH",
            _ => null
        };

        res.Je_nespolehlivym_platcem_DPHKod = f.Je_nespolehlivym_platcem_DPHKod switch
        {
            1 => "Je nespolehlivým plátcem DPH",
            _ => null
        };

        res.Ma_dluh_vzp = f.Ma_dluh_vzpKod switch
        {
            1 => "Má dluh vůči VZP",
            _ => null
        };
        res.Osoby_s_vazbou_na_firmu = f.Osoby_v_OR(aktualnost)
                .DistinctBy(m => m.o.NameId)
                .OrderBy(m => m.o.Prijmeni)
                .ThenBy(m => m.o.Jmeno)
                .ThenBy(m => m.o.Narozeni)
                .Select(m => m.o.ToApiOsobaListItem())
                .ToArray();

        res.Dcerine_spolecnosti = f.IcosInHolding(aktualnost)
            .Select(m => new SimpleDetailInfo()
            {
                Ico = m,
                Jmeno = HlidacStatu.Repositories.Firmy.GetJmeno(m),
                Source_Url = null,
                Copyright = null
            })
            .ToArray();
        
        return res;
    }

    private static async Task<Firma> NajdiFirmuPodleIcoJmenaAsync(string ico, string name,
        DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny)
    {
        Firma f = null;
        if (HlidacStatu.Util.DataValidators.CheckCZICO(ico) == false)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var fname = Firma.JmenoBezKoncovky(name);
                var found = await FirmaRepo.Searching.FindAllAsync(name, 5, true);

                List<(Firma f, int diffs)> diff = found
                    .Select(m => (m, HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(name, m.Jmeno.Trim())))
                    .ToList();
                if (diff.Any(m => m.diffs == 0) == false)
                    diff = found
                        .Select(m => (m,
                            HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(fname, m.JmenoBezKoncovky())))
                        .ToList();
                if (diff.Any(m => m.diffs == 0) == false)
                    diff = found
                        .Select(m => (m,
                            HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(fname.Trim().ToLower(),
                                m.JmenoBezKoncovky().Trim().ToLower())))
                        .ToList();

                if (diff.Any(m => m.diffs == 0))
                    f = diff.First(m => m.diffs == 0).f;
                else if (diff.Any(m => m.diffs <= 1))
                {
                    f = diff.First(m => m.diffs <= 1).f;
                }
            }
        }
        else
        {
            f = Firmy.Get(ico);
        }

        return f;
    }
}