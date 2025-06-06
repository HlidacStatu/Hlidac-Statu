﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Devmasters.Collections;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Nest;
using Consts = HlidacStatu.Entities.KIndex.Consts;

namespace HlidacStatu.Repositories.Analysis.KorupcniRiziko
{
    public partial class Calculator
    {
        const int OBOR_BLACKLIST_bankovnirepo = 11406;
        const int OBOR_BLACKLIST_finance_formality = 11407;

        const int minPocetSmluvKoncentraceDodavateluProZahajeniVypoctu = 1;

        private Firma urad = null;
        StatisticsSubjectPerYear<Smlouva.Statistics.Data> _calc_Stat = null;

        public string Ico { get; private set; }

        private KIndexData kindex = null;

        private Calculator(string ico)
        {
            Ico = ico;

            urad = Firmy.Get(this.Ico);
            if (urad.Valid == false)
                throw new ArgumentOutOfRangeException("invalid ICO");
        }

        public static async Task<Calculator> CreateCalculatorAsync(string ico, bool useTemp)
        {
            var calculator = new Calculator(ico);
            calculator.kindex = await KIndexRepo.GetDirectAsync((ico, useTemp));
            return calculator;
        }

        
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);

        public async Task<KIndexData> GetDataAsync(bool refreshData = false, bool forceCalculateAllYears = false)
        {
            if (refreshData || forceCalculateAllYears)
                this.kindex = null;
            
            await _semaphoreSlim.WaitAsync();
            try
            {
                if (this.kindex == null)
                {
                    this.kindex = await CalculateSourceDataAsync(forceCalculateAllYears);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return kindex;
        }

        private async Task<KIndexData> CalculateSourceDataAsync(bool forceCalculateAllYears)
        {
            this.InitData();
            foreach (var year in Consts.ToCalculationYears)
            {
                KIndexData.Annual data_rok = await CalculateForYearAsync(year, forceCalculateAllYears);
                kindex.roky.Add(data_rok);
            }

            return kindex;
        }

        public void InitData()
        {
            kindex = new KIndexData();
            kindex.Ico = urad.ICO;
            kindex.Jmeno = urad.Jmeno;
            kindex.UcetniJednotka = KIndexData.UcetniJednotkaInfo.Load(urad.ICO);

            _calc_Stat = urad.StatistikaRegistruSmluv();
        }


        static object _koncetraceDodavateluOboryLock = new object();

        //todo: dalo by se ještě refaktorovat, aby se vše bralo ze statistiky
        public async Task<KIndexData.Annual> CalculateForYearAsync(int year, bool forceCalculateAllYears)
        {
            if (_calc_Stat?[year] == null)
                InitData();

            decimal smlouvyZaRok = (decimal)urad.StatistikaRegistruSmluv()[year].PocetSmluv;

            KIndexData.Annual ret = new KIndexData.Annual(year);
            var fc = new FinanceDataCalculator(this.Ico, year);
            ret.FinancniUdaje = fc.GetData();

            //tohle by se dalo brát ze statistiky komplet...
            ret.PercSeZasadnimNedostatkem = smlouvyZaRok == 0
                ? 0m
                : (decimal)_calc_Stat[year].PocetSmluvSeZasadnimNedostatkem / smlouvyZaRok;
            ret.PercSmlouvySPolitickyAngazovanouFirmou =
                _calc_Stat[year].PercentSmluvPolitiky; //this.urad.StatistikaRegistruSmluv()[year].PercentSmluvPolitiky;

            ret.PercNovaFirmaDodavatel =
                smlouvyZaRok == 0 ? 0m : (decimal)_calc_Stat[year].PocetSmluvNovaFirma / smlouvyZaRok;
            ret.PercSmluvUlimitu = smlouvyZaRok == 0 ? 0m : (decimal)_calc_Stat[year].PocetSmluvULimitu / smlouvyZaRok;

            if (year < 2022)
            {
                ret.PercUzavrenoOVikendu =
                  smlouvyZaRok == 0 ? 0m : (decimal)_calc_Stat[year].PocetSmluvOVikendu / smlouvyZaRok;
            }
            else
            {
                ret.PercZacerneno=
                   smlouvyZaRok == 0 ? 0m : (decimal)_calc_Stat[year].PocetZacernenychSmluv / smlouvyZaRok;

            }
            var stat = this.urad.StatistikaRegistruSmluv()[year];
            //todo: tenhle objekt by mohl vycházet ze stávajícího nového objektu statistiky
            ret.Statistika = new KIndexData.StatistickeUdaje()
            {
                PocetSmluv = stat.PocetSmluv,
                CelkovaHodnotaSmluv = stat.CelkovaHodnotaSmluv,
                PocetSmluvBezCeny = stat.PocetSmluvBezCeny,
                PocetSmluvBezSmluvniStrany = stat.PocetSmluvBezSmluvniStrany,
                PocetSmluvPolitiky = stat.PocetSmluvSponzorujiciFirmy,
                PercentSmluvBezCeny = stat.PercentSmluvBezCeny,
                PercentSmluvBezSmluvniStrany = stat.PercentSmluvBezSmluvniStrany,
                PercentKcBezSmluvniStrany = stat.PercentKcBezSmluvniStrany,
                PercentKcSmluvPolitiky = stat.PercentKcSmluvPolitiky,
                PercentSmluvPolitiky = stat.PercentSmluvPolitiky,
                SumKcSmluvBezSmluvniStrany = stat.SumKcSmluvBezSmluvniStrany,
                SumKcSmluvPolitiky = stat.SumKcSmluvSponzorujiciFirmy,
                PocetSmluvULimitu = stat.PocetSmluvULimitu,
                PocetSmluvOVikendu = stat.PocetSmluvOVikendu,
                PocetZacernenychSmluv = stat.PocetZacernenychSmluv,
                PocetSmluvSeZasadnimNedostatkem = stat.PocetSmluvSeZasadnimNedostatkem,
                PocetSmluvNovaFirma = stat.PocetSmluvNovaFirma,
            };


            string queryPlatce = $"icoPlatce:{this.Ico} AND datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}}";

            if (smlouvyZaRok >= minPocetSmluvKoncentraceDodavateluProZahajeniVypoctu)
            {
                IEnumerable<Calculator.SmlouvyForIndex> allSmlouvy = (await GetSmlouvyAsync(queryPlatce)).ToArray();
                IEnumerable<Calculator.SmlouvyForIndex> allSmlouvy_BezBLACKLIST_Obor = allSmlouvy
                    .Where(m => m.Obor != OBOR_BLACKLIST_bankovnirepo && m.Obor != OBOR_BLACKLIST_finance_formality)
                    .ToArray();

                ret.SmlouvyVeVypoctu = allSmlouvy.Select(m => m.Id).ToArray();
                ret.SmlouvyVeVypoctuIgnorovane = allSmlouvy
                    .Where(m => m.Obor == OBOR_BLACKLIST_bankovnirepo || m.Obor == OBOR_BLACKLIST_finance_formality)
                    .Select(m => m.Id).ToArray();

                ret.Statistika.PocetSmluvSeSoukromymSubj = allSmlouvy.Count();
                ret.Statistika.PocetSmluvBezCenySeSoukrSubj = allSmlouvy.Where(m => m.HodnotaSmlouvy == 0).Count();
                ret.Statistika.CelkovaHodnotaSmluvSeSoukrSubj = allSmlouvy.Sum(m => m.HodnotaSmlouvy);

                if (allSmlouvy_BezBLACKLIST_Obor.Any(m => m.HodnotaSmlouvy > 0))
                    ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj = allSmlouvy_BezBLACKLIST_Obor
                        .Where(m => m.HodnotaSmlouvy > 0)
                        .Average(m => m.HodnotaSmlouvy);
                else
                    ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj = 0;

                ret.CelkovaKoncentraceDodavatelu = Calculator.KoncentraceDodavateluCalculator(allSmlouvy_BezBLACKLIST_Obor,
                    queryPlatce, "Koncentrace soukromých dodavatelů", minPocetSmluvToCalculate: 5);
                if (ret.CelkovaKoncentraceDodavatelu != null)
                {
                    if (ret.CelkovaKoncentraceDodavatelu != null)
                    {
                        //ma cenu koncentraci pocitat?
                        //musi byt vice ne 7 smluv a nebo jeden dodavatel musi mit vice nez 2 smlouvy 
                        if (
                            (allSmlouvy_BezBLACKLIST_Obor.Where(m => m.HodnotaSmlouvy == 0).Count() > 0)
                            &&
                            (allSmlouvy_BezBLACKLIST_Obor.Where(m => m.HodnotaSmlouvy == 0).Count() > 7
                             || allSmlouvy_BezBLACKLIST_Obor.Where(m => m.HodnotaSmlouvy == 0)
                                 .GroupBy(k => k.Dodavatel, v => v, (k, v) => v.Count())
                                 .Max() > 2
                            )
                        )
                        {
                            ret.KoncentraceDodavateluBezUvedeneCeny
                                = Calculator.KoncentraceDodavateluCalculator(
                                    allSmlouvy_BezBLACKLIST_Obor.Where(m => m.HodnotaSmlouvy == 0),
                                    queryPlatce + " AND hint.skrytaCena:1",
                                    "Koncentrace soukromých dodavatelů u smluv s utajenou cenou",
                                    ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj, 5);

                            if (ret.KoncentraceDodavateluBezUvedeneCeny?.Dodavatele != null)
                                ret.KoncentraceDodavateluBezUvedeneCeny.Dodavatele = ret
                                    .KoncentraceDodavateluBezUvedeneCeny
                                    .Dodavatele
                                    //jde o smlouvy bez ceny, u souhrnu dodavatelu resetuj ceny na 0
                                    .Select(m => new KoncentraceDodavateluIndexy.Souhrn()
                                    {
                                        HodnotaSmluv = 0, Ico = m.Ico, PocetSmluv = m.PocetSmluv, Poznamka = m.Poznamka
                                    })
                                    .ToArray();
                        }
                    }

                    if (ret.PercSmluvUlimitu > 0)
                    {
                        if (
                            (allSmlouvy.Where(m => m.ULimitu > 0).Count() > 0)
                            && (
                                allSmlouvy.Where(m => m.ULimitu > 0).Count() > 7
                                || allSmlouvy.Where(m => m.ULimitu > 0)
                                    .GroupBy(k => k.Dodavatel, v => v, (k, v) => v.Count())
                                    .Max() > 2
                            )
                        )
                        {
                            ret.KoncentraceDodavateluCenyULimitu
                                = Calculator.KoncentraceDodavateluCalculator(allSmlouvy.Where(m => m.ULimitu > 0),
                                    queryPlatce + " AND ( hint.smlouvaULimitu:>0 )",
                                    "Koncentrace soukromých dodavatelů u smluv s cenou u limitu veřejných zakázek",
                                    ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj, 5);
                        }
                    }

                    Dictionary<int, string> obory = Smlouva.SClassification
                        .AllTypes
                        .Where(m => m.IsMainType)
                        .OrderBy(m => m.Value)
                        .ToDictionary(k => k.Value, v => v.SearchShortcut);

                    ret.KoncetraceDodavateluObory = new List<KoncentraceDodavateluObor>();

                    Devmasters.Batch.Manager.DoActionForAll<int>(obory.Keys,
                        (oborid) =>
                        {
                            var queryPlatceObor =
                                $"icoPlatce:{this.Ico} AND datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}} AND oblast:{obory[oborid]}";
                            var allSmlouvyObory = allSmlouvy.Where(w => w.ContainsObor(oborid));

                            //vyjimka pro repo obory, ty nepocitat co financi
                            if (oborid == 11400)
                            {
                                queryPlatceObor =
                                    $"icoPlatce:{this.Ico} AND datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}} AND " +
                                    $" classification.class1.typeValue:[11400 TO 11499] AND NOT(classification.class1.typeValue:{OBOR_BLACKLIST_bankovnirepo} OR classification.class1.typeValue:{OBOR_BLACKLIST_finance_formality} ) ";
                                allSmlouvyObory = allSmlouvyObory.Where(w =>
                                    w.Obor != OBOR_BLACKLIST_bankovnirepo &&
                                    w.Obor != OBOR_BLACKLIST_finance_formality);
                            }

                            var k = Calculator.KoncentraceDodavateluCalculator(allSmlouvyObory,
                                queryPlatceObor, "Koncentrace soukromých dodavatelů u oboru " + obory[oborid],
                                ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj);


                            //KoncentraceDodavateluIndexy kbezCeny = null;
                            lock (_koncetraceDodavateluOboryLock)
                            {
                                if (k != null)
                                {
                                    ret.KoncetraceDodavateluObory.Add(new KoncentraceDodavateluObor()
                                    {
                                        OborId = oborid,
                                        OborName = obory[oborid],
                                        Koncentrace = k,
                                        SmluvBezCenyMalusKoeficient = k.PocetSmluvProVypocet == 0
                                            ? 1
                                            : (1m + (decimal)k.PocetSmluvBezCenyProVypocet /
                                                (decimal)k.PocetSmluvProVypocet)
                                        //KoncentraceBezUvedeneCeny = kbezCeny
                                    });
                                }

                                ;
                            }

                            return new Devmasters.Batch.ActionOutputData();
                        }, null, null, !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: 10
                        , prefix: "kindex calc4year "
                        , monitor: new MonitoredTaskRepo.ForBatch()
                    );
                } // if (ret.CelkovaKoncentraceDodavatelu != null)
            }

            ret.TotalAveragePercSmlouvyPod50k = await TotalAveragePercSmlouvyPod50KAsync(ret.Rok);

            ret.PercSmlouvyPod50k = await AveragePercSmlouvyPod50KAsync(this.Ico, ret.Rok, ret.Statistika.PocetSmluv);
            ret.PercSmlouvyPod50kBonus = SmlouvyPod50kBonus(ret.PercSmlouvyPod50k, ret.TotalAveragePercSmlouvyPod50k);


            ret = await FinalCalculationKIdxAsync(ret, forceCalculateAllYears);

            return ret;
        }

        public async Task<KIndexData.Annual> FinalCalculationKIdxAsync(KIndexData.Annual ret, bool forceCalculateAllYears)
        {
            decimal smlouvyZaRok = (decimal)urad.StatistikaRegistruSmluv()[ret.Rok].PocetSmluv;

            CalculateKIndex(ret);

            bool kindexInLimits = (
                (ret.Statistika.PocetSmluvSeSoukromymSubj >= Consts.MinPocetSmluvPerYear
                ||
                (ret.Statistika.CelkovaHodnotaSmluvSeSoukrSubj + ret.Statistika.PrumernaHodnotaSmluvSeSoukrSubj *
                    ret.Statistika.PocetSmluvBezCenySeSoukrSubj) >= Consts.MinSmluvySummaryPerYear
                )
                ||
                (ret.Statistika.CelkovaHodnotaSmluvSeSoukrSubj >= Consts.MinSmluvySummaryPerYear)
            );
            //hard limit. Musi mit alespon 20 smluv v registru
            kindexInLimits = kindexInLimits && (ret.Statistika.PocetSmluvSeSoukromymSubj >= Consts.MinPocetSmluvPerYearIfHasSummarySmluv);


            if (kindexInLimits)
            {
                ret.KIndexReady = true;
                ret.KIndexIssues = null;
            }
            else if (await Firmy.Get(this.Ico).MusiPublikovatDoRSAsync() == false)
            {
                if (forceCalculateAllYears == false)
                {
                    ret.KIndexReady = false;
                    ret.KIndexIssues = new string[]
                    {
                        $"K-Index spočítán, ale organizace ze zákona nemusí do registru smluv publikovat. Publikuje pouze dobrovolně, proto K-Index nezveřejňujeme."
                    };
                }
                else
                {
                    ret.KIndexReady = true;
                    ret.KIndexIssues = new string[]
                    {
                        $"Organizace ze zákona nemusí do registru smluv publikovat. Publikuje pouze dobrovolně, nad rámec zákona."
                    };
                }
            }
            else
            {
                if (forceCalculateAllYears == false)
                {
                    ret.KIndexReady = false;
                    ret.KIndexIssues = new string[]
                    {
                        $"K-Index nespočítán. Méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv."
                    };
                }
                else
                {
                    ret.KIndexReady = true;
                    ret.KIndexIssues = new string[]
                    {
                        $"Organizace má méně smluv nebo objem smluv než určuje metodika. Pro výpočet a publikaci byla aplikována výjimka."
                    };
                }
            }


            ret.LastUpdated = DateTime.Now;

            return ret;
        }

        public void CalculateKIndex(KIndexData.Annual datayear)
        {
            KIndexData.VypocetDetail vypocet = new KIndexData.VypocetDetail();
            var vradky = new List<KIndexData.VypocetDetail.Radek>();


            decimal val =
                //r5
                datayear.Statistika.PercentSmluvBezCeny *
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                    .PercentBezCeny); //=10   C > 1  F > 2,5
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                KIndexData.KIndexParts.PercentBezCeny,
                datayear.Statistika.PercentSmluvBezCeny,
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercentBezCeny))
            );

            //r11
            val += datayear.PercSeZasadnimNedostatkem *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                       .PercSeZasadnimNedostatkem); //=20
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                KIndexData.KIndexParts.PercSeZasadnimNedostatkem,
                datayear.PercSeZasadnimNedostatkem,
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercSeZasadnimNedostatkem))
            );


            //r13
            val += datayear.CelkovaKoncentraceDodavatelu?.Herfindahl_Hirschman_Modified *
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                    .CelkovaKoncentraceDodavatelu) ?? 0; //=40
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                KIndexData.KIndexParts.CelkovaKoncentraceDodavatelu,
                datayear.CelkovaKoncentraceDodavatelu?.Herfindahl_Hirschman_Modified ?? 0,
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.CelkovaKoncentraceDodavatelu))
            );


            //r15
            decimal r15koef = 1;
            if (datayear.Statistika.PercentSmluvBezCeny < 0.05m)
                r15koef = 2m;
            decimal r15val = datayear.KoncentraceDodavateluBezUvedeneCeny?.Herfindahl_Hirschman_Modified ?? 0;
            r15val = r15val / r15koef;

            val += r15val *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                       .KoncentraceDodavateluBezUvedeneCeny); //60
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                KIndexData.KIndexParts.KoncentraceDodavateluBezUvedeneCeny,
                r15val,
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                    .KoncentraceDodavateluBezUvedeneCeny))
            );

            //r17
            val += datayear.PercSmluvUlimitu *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercSmluvUlimitu); //70
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                KIndexData.KIndexParts.PercSmluvUlimitu,
                datayear.PercSmluvUlimitu,
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercSmluvUlimitu))
            );

            //r18
            val += datayear.KoncentraceDodavateluCenyULimitu?.Herfindahl_Hirschman_Modified *
                KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                    .KoncentraceDodavateluCenyULimitu) ?? 0; //80
            vradky.Add(
                new KIndexData.VypocetDetail.Radek(
                    KIndexData.KIndexParts.KoncentraceDodavateluCenyULimitu,
                    datayear.KoncentraceDodavateluCenyULimitu?.Herfindahl_Hirschman_Modified ?? 0,
                    KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                        .KoncentraceDodavateluCenyULimitu))
            );

            //r19
            val += datayear.PercNovaFirmaDodavatel *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                       .PercNovaFirmaDodavatel); //82
            vradky.Add(
                new KIndexData.VypocetDetail.Radek(KIndexData.KIndexParts.PercNovaFirmaDodavatel,
                    datayear.PercNovaFirmaDodavatel,
                    KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercNovaFirmaDodavatel))
            );


            if (datayear.Rok < 2022)
            {             //r21
                val += datayear.PercUzavrenoOVikendu *
                       KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercUzavrenoOVikendu); //84
                vradky.Add(
                    new KIndexData.VypocetDetail.Radek(KIndexData.KIndexParts.PercUzavrenoOVikendu,
                        datayear.PercUzavrenoOVikendu,
                        KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercUzavrenoOVikendu))
                );
            }
            else {
                val += datayear.PercZacerneno *
                       KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercZacerneno); //84
                vradky.Add(
                    new KIndexData.VypocetDetail.Radek(KIndexData.KIndexParts.PercZacerneno,
                        datayear.PercZacerneno,
                        KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercZacerneno))
                );
            }
            //r22
            val += datayear.PercSmlouvySPolitickyAngazovanouFirmou *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                       .PercSmlouvySPolitickyAngazovanouFirmou); //86
            vradky.Add(new KIndexData.VypocetDetail.Radek(
                    KIndexData.KIndexParts.PercSmlouvySPolitickyAngazovanouFirmou,
                    datayear.PercSmlouvySPolitickyAngazovanouFirmou,
                    KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                        .PercSmlouvySPolitickyAngazovanouFirmou)
                )
            );

            if (datayear.KoncetraceDodavateluObory != null)
            {
                //oborova koncentrace
                var oboryKoncentrace = datayear.KoncetraceDodavateluObory
                    //.Where(m=>m != null)
                    .Where(m =>
                        m.Koncentrace.HodnotaSmluvProVypocet > (datayear.Statistika.CelkovaHodnotaSmluv * 0.05m)
                        || m.Koncentrace.PocetSmluvProVypocet > (datayear.Statistika.PocetSmluv * 0.05m)
                        || (m.Koncentrace.PocetSmluvBezCenyProVypocet > datayear.Statistika.PocetSmluvBezCeny * 0.02m)
                    )
                    .ToArray(); //for better debug;

                decimal prumernaCenaSmluv = datayear.Statistika.PrumernaHodnotaSmluvSeSoukrSubj;
                var oboryVahy = oboryKoncentrace
                    .Select(m => new KIndexData.VypocetOboroveKoncentrace.RadekObor()
                    {
                        Obor = m.OborName,
                        Hodnota = m.Combined_Herfindahl_Hirschman_Modified(),
                        Vaha = m.Koncentrace.HodnotaSmluvProVypocet > 0
                            ? m.Koncentrace.HodnotaSmluvProVypocet
                            : 1, // prumerne cenu u nulobych cen uz pocitam u Koncentrace.HodnotaSmluvProVypocet //+ (prumernaCenaSmluv * (decimal)m.Koncentrace.PocetSmluvProVypocet * m.PodilSmluvBezCeny),
                        PodilSmluvBezCeny = m.PodilSmluvBezCeny,
                        CelkovaHodnotaSmluv = m.Koncentrace.HodnotaSmluvProVypocet,
                        PocetSmluvCelkem = m.Koncentrace.PocetSmluvProVypocet
                    })
                    .ToArray();
                decimal avg = oboryVahy.WeightedAverage(m => m.Hodnota, w => w.Vaha);
                val += avg *
                       KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                           .KoncentraceDodavateluObory);
                vradky.Add(
                    new KIndexData.VypocetDetail.Radek(KIndexData.KIndexParts.KoncentraceDodavateluObory, avg,
                        KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts
                            .KoncentraceDodavateluObory))
                );
                vypocet.OboroveKoncentrace = new KIndexData.VypocetOboroveKoncentrace();
                vypocet.OboroveKoncentrace.PrumernaCenaSmluv = prumernaCenaSmluv;
                vypocet.OboroveKoncentrace.Radky = oboryVahy.ToArray();
            }

            //
            //r16 - bonus!
            val -= datayear.PercSmlouvyPod50kBonus *
                   KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercSmlouvyPod50kBonus);

            vradky.Add(new KIndexData.VypocetDetail.Radek(
                    KIndexData.KIndexParts.PercSmlouvyPod50kBonus, -1 * datayear.PercSmlouvyPod50kBonus,
                    KIndexData.DetailInfo.DefaultKIndexPartKoeficient(KIndexData.KIndexParts.PercSmlouvyPod50kBonus)
                )
            );
            vypocet.Radky = vradky.ToArray();

            if (val < 0)
                val = 0;

            var kontrolniVypocet = vypocet.Vypocet();
            if (val != kontrolniVypocet)
                throw new ApplicationException("Nesedi vypocet");

            vypocet.LastUpdated = DateTime.Now;
            datayear.KIndexVypocet = vypocet;


            datayear.KIndex = val;
        }


        public static async Task<decimal> AveragePercSmlouvyPod50KAsync(string ico, int year, long pocetSmluvCelkem)
        {
            if (pocetSmluvCelkem == 0)
                return 0;

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                $"ico:{ico} cena:>0 AND cena:<=50000 AND datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}}"
                , 1, 0, SmlouvaRepo.Searching.OrderResult.FastestForScroll, null, exactNumOfResults: true);

            decimal perc = (decimal)(res.Total) / (decimal)pocetSmluvCelkem;
            return perc;
        }


        static Dictionary<int, decimal> totalsAvg50k = new Dictionary<int, decimal>();
        static object totalsAvg50kLock = new object();
        private static SemaphoreSlim _totalsAvg50kSemaphore = new SemaphoreSlim(1, 1);

        public static async Task<decimal> TotalAveragePercSmlouvyPod50KAsync(int year)
        {
            if (!totalsAvg50k.ContainsKey(year))
            {
                await _totalsAvg50kSemaphore.WaitAsync();
                try
                {
                    if (!totalsAvg50k.ContainsKey(year))
                    {
                        decimal smlouvyAllCount = 0;
                        decimal smlouvyPod50kCount = 0;
                        var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                            $"datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}}"
                            , 1, 0, SmlouvaRepo.Searching.OrderResult.FastestForScroll, null, exactNumOfResults: true);
                        if (res.IsValid)
                            smlouvyAllCount = res.Total;
                        res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                            $"cena:>0 AND cena:<=50000 AND datumUzavreni:[{year}-01-01 TO {year + 1}-01-01}}"
                            , 1, 0, SmlouvaRepo.Searching.OrderResult.FastestForScroll, null, exactNumOfResults: true);
                        if (res.IsValid)
                            smlouvyPod50kCount = res.Total;


                        decimal smlouvyPod50kperc = 0;

                        if (smlouvyAllCount > 0)
                            smlouvyPod50kperc = smlouvyPod50kCount / smlouvyAllCount;

                        totalsAvg50k.Add(year, smlouvyPod50kperc);
                    }
                }
                finally
                {
                    _totalsAvg50kSemaphore.Release();
                }
            }

            return totalsAvg50k[year];
        }

        public static decimal SmlouvyPod50kBonus(decimal icoPodil, decimal vsePodil)
        {
            if (icoPodil > vsePodil * 1.75m)
                return Consts.BonusPod50K_3;
            else if (icoPodil > vsePodil * 1.5m)
                return Consts.BonusPod50K_2;
            if (icoPodil > vsePodil * 1.25m)
                return Consts.BonusPod50K_1;
            return 0m;
        }

        class smlouvaStat
        {
            public string Id { get; set; }
            public string IcoDodavatele { get; set; }
            public decimal CastkaSDPH { get; set; }
            public int Rok { get; set; }
            public DateTime Podepsano { get; set; }
            public int ULimitu { get; set; }
            public int Obor { get; set; }
        }

        public async Task<IEnumerable<Calculator.SmlouvyForIndex>> GetSmlouvyAsync(string query)
        {
            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = async (size, page) =>
            {
                var client = Manager.GetESClient();
                return await client.SearchAsync<Smlouva>(a => a
                    .Size(size)
                    .Source(ss => ss.Excludes(sml => sml.Field(ff => ff.Prilohy)))
                    .From(page * size)
                    .Query(q => SmlouvaRepo.Searching.GetSimpleQuery(query))
                    .Scroll("1m")
                );
            };
                

            List<smlouvaStat> smlStat = new List<smlouvaStat>();
            await Repositories.Searching.Tools.DoActionForQueryAsync<Smlouva>(Manager.GetESClient(),
                searchFunc,
                (h, o) =>
                {
                    Smlouva s = h.Source;
                    if (s != null)
                    {
                        foreach (var prij in s.Prijemce)
                        {
                            if (prij.ico == s.Platce.ico)
                                continue;
                            Firma f = Firmy.Get(prij.ico);
                            if (f.Valid && f.PatrimStatuAlespon25procent())
                                continue;

                            string dodavatel = prij.ico;
                            if (string.IsNullOrWhiteSpace(dodavatel))
                                dodavatel = prij.nazev;
                            if (string.IsNullOrWhiteSpace(dodavatel))
                                dodavatel = "neznamy";

                            smlStat.Add(new smlouvaStat()
                            {
                                Id = s.Id,
                                IcoDodavatele = dodavatel,
                                CastkaSDPH = Math.Abs((decimal)s.CalculatedPriceWithVATinCZK /
                                                      (decimal)s.Prijemce.Length),
                                Podepsano = s.datumUzavreni,
                                Rok = s.datumUzavreni.Year,
                                ULimitu = s.Hint?.SmlouvaULimitu ?? 0,
                                Obor = s.Classification.Class1?.TypeValue ?? 0
                            });
                        }
                    }

                    return new Devmasters.Batch.ActionOutputData();
                }, null,
                null, null,
                false, blockSize: 100,
                monitor: new MonitoredTaskRepo.ForBatch());

            IEnumerable<Calculator.SmlouvyForIndex> smlouvy = smlStat
                .Select(m => new Calculator.SmlouvyForIndex(m.Id, m.IcoDodavatele, m.CastkaSDPH, m.ULimitu, m.Obor))
                .OrderByDescending(m => m.HodnotaSmlouvy) //just better debug
                .ToArray(); //just better debug

            return smlouvy;
        }
    }
}