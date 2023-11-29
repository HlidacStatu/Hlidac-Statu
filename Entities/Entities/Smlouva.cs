using HlidacStatu.Entities.Issues;
using HlidacStatu.Entities.XSD;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace HlidacStatu.Entities
{

    public partial class Smlouva
        : IBookmarkable, IFlattenedExport
    {

        public tIdentifikator identifikator;

        object enhLock = new object();
        private Issues.Issue[] issues = new Issues.Issue[] { };
        object lockInfoObj = new object();
        public Smlouva()
        { }

        public Smlouva(dumpZaznam item)
        {
            casZverejneni = item.casZverejneni;
            identifikator = item.identifikator;
            odkaz = item.odkaz;
            platnyZaznam = item.platnyZaznam == 1;
            if (item.prilohy != null)
                Prilohy = item.prilohy.Select(m => new Priloha(m)).ToArray();

            cisloSmlouvy = item.smlouva.cisloSmlouvy;
            ciziMena = item.smlouva.ciziMena;
            datumUzavreni = item.smlouva.datumUzavreni;
            hodnotaBezDph = item.smlouva.hodnotaBezDphSpecified ? item.smlouva.hodnotaBezDph : (decimal?)null;
            hodnotaVcetneDph = item.smlouva.hodnotaVcetneDphSpecified ? item.smlouva.hodnotaVcetneDph : (decimal?)null;

            predmet = item.smlouva.predmet;
            schvalil = item.smlouva.schvalil;

            navazanyZaznam = item.smlouva.navazanyZaznam;
            znepristupnitPredchoziZaznam = item.smlouva.znepristupnitPredchoziZaznam ?? false;
            cenaNeuvedenaDuvod = item.smlouva.cenaNeuvedena;

            //smluvni strany

            //vkladatel je jasny
            VkladatelDoRejstriku = new Subjekt(item.smlouva.subjekt);

            //pokud je nastaven parametr
            //<xs:documentation xml:lang="cs">1 = příjemce, 0 = plátce</xs:documentation>
            bool platceSpecified = item.smlouva.smluvniStrana.Any(m => m.prijemce.HasValue && m.prijemce == false);
            bool prijemceSpecified = item.smlouva.smluvniStrana.Any(m => m.prijemce.HasValue && m.prijemce == true);

            if (platceSpecified)
                Platce =
                    new Subjekt(item.smlouva
                        .smluvniStrana
                        .Where(m => m.prijemce.HasValue && m.prijemce == false)
                        .First()
                        );
            else
            {
                //copy from subjekt
                Platce = new Subjekt(item.smlouva.subjekt);
            }

            if (prijemceSpecified)
            {
                Prijemce = item.smlouva.smluvniStrana
                    .Where(m => m.prijemce.HasValue && m.prijemce == true)
                    .Select(m => new Subjekt(m))
                    .ToArray();

                //add missing from source
                Prijemce = Prijemce
                                        .ToArray()
                                        .Union(
                                                item.smlouva.smluvniStrana
                                                    .Where(m => m.prijemce.HasValue == false)
                                                    .Select(m => new Subjekt(m))
                                        ).ToArray();


            }
            else
            {
                Prijemce = item.smlouva.smluvniStrana
                    //.Where(m => m.ico != this.Platce.ico || m.datovaSchranka != this.Platce.datovaSchranka)
                    .Where(m => m.prijemce.HasValue == false && m.prijemce != false)
                    .Select(m => new Subjekt(m))
                    .ToArray();
            }

            //add missing from subject
            if (platceSpecified)
            {
                if (Prijemce
                        .Where(m => m.ico == VkladatelDoRejstriku.ico || m.datovaSchranka != VkladatelDoRejstriku.datovaSchranka)
                        .Count() == 0
                    )
                {
                    Prijemce = Prijemce
                                        .ToArray()
                                        .Union(
                                                new Subjekt[] { VkladatelDoRejstriku }
                                        ).ToArray();
                }

            }

        }

        public enum PravniRamce
        {
            Undefined = 0,
            Do072017 = 1,
            Od072017 = 2,
            MimoRS = 3
        }

        public decimal CalculatedPriceWithVATinCZK { get; set; }
        public DataQualityEnum CalcutatedPriceQuality { get; set; }
        [Date]
        public DateTime casZverejneni { get; set; }

        [Keyword]
        public string cisloSmlouvy { get; set; }

        public tSmlouvaCiziMena ciziMena { get; set; }
        public SClassification Classification { get; set; } = new SClassification();
        [Number]
        public decimal ConfidenceValue { get; set; }

        [Date]
        public DateTime datumUzavreni { get; set; }

        public Enhancers.Enhancement[] Enhancements { get; set; } = new Enhancers.Enhancement[] { };
        public decimal? hodnotaBezDph { get; set; }

        public decimal? hodnotaVcetneDph { get; set; }

        [Text]
        public string Id
        {
            get
            {
                return identifikator?.idVerze;  //todo: es7 __ ?. __ added, because identifikator was null
            }
        }

        // { get; set; }

        public Issues.Issue[] Issues
        {
            get
            {
                return issues;
            }
            set
            {

                if (issues.Any(m => m.Permanent))
                {
                    //nech jen permanent
                    var newIss = issues.Where(m => m.Permanent).ToList();
                    //unique Ids, at se neopakuji
                    var existsIds = newIss.Select(m => m.IssueTypeId).Distinct();

                    //pridej vse krome existujicich Ids
                    newIss.AddRange(value.Where(m => !(existsIds.Contains(m.IssueTypeId))));
                    issues = newIss.ToArray();
                }
                else
                    issues = value;
                ConfidenceValue = GetConfidenceValue();
            }
        }

        [Date]
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;

        [Keyword]
        public string navazanyZaznam { get; set; }

        [Nest.Boolean]
        public bool znepristupnitPredchoziZaznam { get; set; } = false;

        public string cenaNeuvedenaDuvod { get; set; } = null;

        [Keyword]
        public string odkaz { get; set; }
        public Subjekt Platce { get; set; }
        /// <remarks/>
        public bool platnyZaznam { get; set; }

        [Object(Ignore = true)]
        public PravniRamce PravniRamec
        {
            get
            {
                if (IsPartOfRegistrSmluv() == false)
                {
                    return PravniRamce.MimoRS;
                }
                else
                {
                    if (datumUzavreni < pravniRamce01072017)
                        return PravniRamce.Do072017;
                    else if (datumUzavreni >= pravniRamce01072017)
                        return PravniRamce.Od072017;
                    else
                        return PravniRamce.Undefined;
                }
            }
        }

        [Text]
        public string predmet { get; set; }
        

        public Subjekt[] Prijemce { get; set; }
        public Priloha[] Prilohy { get; set; }
        [Keyword]
        public string schvalil { get; set; }

        [Keyword]
        public string[] souvisejiciSmlouvy { get; set; } = null;

        //public tSmlouva smlouva;
        public bool spadaPodRS { get; set; } = true;

        [Boolean()]
        public bool? SVazbouNaPolitiky { get; set; } = false;

        [Boolean()]
        public bool? SVazbouNaPolitikyAktualni { get; set; } = false;

        [Boolean()]
        public bool? SVazbouNaPolitikyNedavne { get; set; } = false;

        public HintSmlouva Hint { get; set; }

        public Subjekt VkladatelDoRejstriku { get; set; }
        public void AddEnhancement(Enhancers.Enhancement enh)
        {
            lock (enhLock)
            {
                if (!Enhancements.Contains(enh))
                {
                    //add new to the array http://stackoverflow.com/a/31542691/1906880
                    Enhancers.Enhancement[] result = new Enhancers.Enhancement[Enhancements.Length + 1];
                    Enhancements.CopyTo(result, 0);
                    result[Enhancements.Length] = enh;
                }
                else
                {
                    var existingIdx = Array.FindIndex(Enhancements, e => e == enh);
                    if (existingIdx > -1)
                    {
                        Enhancements[existingIdx] = enh;
                    }

                }
            }
        }

        public void AddSpecificIssue(Issues.Issue i)
        {
            if (!Issues.Any(m => m.IssueTypeId == i.IssueTypeId))
            {
                var oldIssues = Issues.ToList();
                oldIssues.Add(i);
                Issues = oldIssues.ToArray();
            }

        }

        public string BookmarkName()
        {
            return predmet;
        }

        public void ClearAllIssuesIncludedPermanent()
        {
            issues = new Issues.Issue[] { };
        }

        public string FullTitle()
        {
            return string.Format("Smlouva č. {0}: {1}", Id, Devmasters.TextUtil.ShortenText(predmet ?? "", 70));
        }

        public ImportanceLevel GetConfidenceLevel()
        {
            if (ConfidenceValue <= 0 || Issues == null)
            {
                return ImportanceLevel.Ok;
            }
            if (Issues.Count() == 0)
            {
                return ImportanceLevel.Ok;
            }
            //pokud je tam min 1x fatal, je cele fatal
            if (Issues.Any(m => m.Importance == ImportanceLevel.Fatal))
            {
                return ImportanceLevel.Fatal;
            }
            if (ConfidenceValue > ((int)ImportanceLevel.Major) * 3)
            {
                return ImportanceLevel.Fatal;
            }


            //pokud je tam min 1x fatal, je cele fatal
            if (Issues.Any(m => m.Importance == ImportanceLevel.Major))
            {
                return ImportanceLevel.Major;
            }
            if (ConfidenceValue > ((int)ImportanceLevel.Major) * 2 && ConfidenceValue <= ((int)ImportanceLevel.Major) * 3)
            {
                return ImportanceLevel.Major;
            }

            if (ConfidenceValue > ((int)ImportanceLevel.Minor) * 2 && ConfidenceValue <= ((int)ImportanceLevel.Major) * 2)
            {
                return ImportanceLevel.Minor;
            }

            if (ConfidenceValue > 0 && ConfidenceValue <= ((int)ImportanceLevel.Minor) * 2)
            {
                return ImportanceLevel.Formal;
            }

            return ImportanceLevel.Ok;
        }



        public SClassification.Classification[] GetRelevantClassification()
        {
            Classification = Classification ?? new SClassification();
            var types = Classification.GetClassif();
            return types.ToArray();
        }

        public string GetUrl(bool local = true)
        {
            return GetUrl(local, string.Empty);
        }

        public string GetUrl(bool local, string foundWithQuery)
        {
            string url = "/Detail/" + Id;// E49DE92B876B0C66C2F29457EB61C7B7
            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

            if (local == false)
                return "https://www.hlidacstatu.cz" + url;
            else
                return url;
        }



        public bool IsPartOfRegistrSmluv()
        {
            if (Id != null && Id.StartsWith("pre")) //todo: es7 __this.Id != null && __ added
                return false;
            else
                return true;
        }

        public string NicePrice(bool html = false)
        {
            string res = "Neuvedena";
            if (CalculatedPriceWithVATinCZK == 0)
                return res;
            else
                return NicePrice(CalculatedPriceWithVATinCZK, html: html);
        }




        public bool NotInterestingToShow() { return false; }

        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectId()
        {
            return Id;
        }

        public string ToAuditObjectTypeName()
        {
            return "Smlouva";
        }

        public bool znepristupnenaSmlouva()
        {
            var b = Issues != null && Issues.Any(m => m.IssueTypeId == -1);
            if (IsPartOfRegistrSmluv() == false)
                b = false;
            return (b || platnyZaznam == false);
        }





        public SClassification.Classification[] relevantClassif(SClassification.Classification[] types)
        {
            types = types ?? new SClassification.Classification[] { };
            var firstT = types.OrderByDescending(m => m.ClassifProbability)
                .Where(m => m.ClassifProbability >= SClassification.MinAcceptablePoints)
                .FirstOrDefault();
            if (firstT == null)
                return new SClassification.Classification[] { };
            var secondT = types.OrderByDescending(m => m.ClassifProbability)
                .Skip(1)
                .Where(m => m.ClassifProbability >= SClassification.MinAcceptablePointsSecond)
                .FirstOrDefault();

            var thirdT = types.OrderByDescending(m => m.ClassifProbability)
                .Skip(2)
                .Where(m => m.ClassifProbability >= SClassification.MinAcceptablePointsThird)
                .FirstOrDefault();


            SClassification.Classification[] vals = new SClassification.Classification[] { firstT, secondT, thirdT };
            return vals.Where(m => m != null).ToArray();

        }
        public decimal GetConfidenceValue()
        {
            if (IsPartOfRegistrSmluv() == false)
                return 0;

            if (Issues != null)
                return Issues.Sum(m => (int)m.Importance);
            else
            {
                return 0;
            }

        }


        public static Smlouva Export(Smlouva smlouva, bool allData = false, bool docsContent = true)
        {
            Smlouva s = (Smlouva)smlouva.MemberwiseClone();
            if (s == null)
                return null;

            if (s.znepristupnenaSmlouva() && s.Prilohy != null)
            {
                foreach (var p in s.Prilohy)
                {
                    p.PlainTextContent = "-- anonymizovano serverem HlidacStatu.cz --";
                    p.odkaz = "";
                }
            }

            if (allData == false)
            {
                if (s.Prilohy != null)
                {
                    foreach (var p in s.Prilohy)
                    {
                        p.FileMetadata = null;
                    }
                }
                s.Classification = null;
                s.SVazbouNaPolitiky = null;
                s.SVazbouNaPolitikyAktualni = null;
                s.SVazbouNaPolitikyNedavne = null;
            }
            if (docsContent == false)
                if (s.Prilohy != null)
                {
                    foreach (var p in s.Prilohy)
                    {
                        p.PlainTextContent = null;
                    }
                }

            if (allData == false)
            {
                //Composite formatting { escaping

                //var licence = "\"{0}\":{{ \"note\":\"-- Tato data jsou dostupná pouze v komerční nebo speciální licenci. Kontaktujte nás. --\" }}";
                //ret = HlidacStatu.Util.ParseTools.GetStringReplaceWithRegex("\"classification\": \\s? null", ret, string.Format(licence, "classification"));
                //ret = HlidacStatu.Util.ParseTools.GetStringReplaceWithRegex("\"sVazbouNaPolitiky\": \\s? null", ret, string.Format(licence, "sVazbouNaPolitiky"));
                //ret = HlidacStatu.Util.ParseTools.GetStringReplaceWithRegex("\"sVazbouNaPolitikyNedavne\": \\s? null", ret, string.Format(licence, "sVazbouNaPolitikyNedavne"));
                //ret = HlidacStatu.Util.ParseTools.GetStringReplaceWithRegex("\"sVazbouNaPolitikyAktualni\": \\s? null", ret, string.Format(licence, "sVazbouNaPolitikyAktualni"));
                s.Classification = null;
                s.SVazbouNaPolitiky = null;
                s.SVazbouNaPolitikyAktualni = null;
                s.SVazbouNaPolitikyNedavne = null;
            }

            return s;
        }

        //migrace: tohle nahradit pomocí devmasters tools pro nice price.

        public static string NicePrice(decimal? number, string mena = "Kč", bool html = false, bool shortFormat = false)
        {
            if (number.HasValue)
                return NicePrice(number.Value, mena, html, shortFormat);
            else
                return string.Empty;
        }

        public static string NicePrice(double? number, string mena = "Kč", bool html = false, bool shortFormat = false)
        {
            if (number.HasValue)
                return NicePrice((decimal)number.Value, mena, html, shortFormat);
            else
                return string.Empty;
        }

        public static string NicePrice(decimal number, string mena = "Kč", bool html = false, bool shortFormat = false)
        {
            return RenderData.NicePrice(number, mena: mena, html: html, shortFormat: shortFormat);
        }

        public static string ShortNicePrice(decimal number, string mena = "Kč", bool html = false, bool shortFormat = false)
        {
            return RenderData.ShortNicePrice(number, mena: mena, html: html);
        }

        public ExpandoObject FlatExport()
        {
            dynamic v = new ExpandoObject();
            v.url = GetUrl(false);
            v.id = Id;
            v.predmet = predmet;
            v.datumUzavreni = datumUzavreni;
            v.casZverejneni = casZverejneni;
            v.hodnotaSmlouvy_sDPH = CalculatedPriceWithVATinCZK;
            v.platceJmeno = Platce.nazev;
            v.platceIco = Platce.ico;
            for (int i = 0; i < Prijemce.Count(); i++)
            {
                ((IDictionary<String, Object>)v).Add($"prijemceJmeno_{i + 1}", Prijemce[i].nazev);
                ((IDictionary<String, Object>)v).Add($"prijemceIco_{i + 1}", Prijemce[i].ico);
            }
            for (int i = 0; i < Issues.Count(); i++)
            {
                ((IDictionary<String, Object>)v).Add($"chyba_{i + 1}", Issues[i].Title);
            }
            return v;
        }

        static DateTime pravniRamce01072017 = new DateTime(2017, 7, 1);

    }
}
