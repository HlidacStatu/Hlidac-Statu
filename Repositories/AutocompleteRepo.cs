using Devmasters.Batch;
using Devmasters.Enums;

using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using Consts = HlidacStatu.Util.Consts;

namespace HlidacStatu.Repositories
{
    //migrace: tohle není repo - přestěhovat jinam => až budeme vytvářet samostatnou api
    public static class AutocompleteRepo
    {

        //static Devmasters.Batch.ActionProgressWriter progressWriter = new Devmasters.Batch.ActionProgressWriter(0.1f, Devmasters.Batch.ProgressWriters.ConsoleWriter_EndsIn);

        static bool debug = System.Diagnostics.Debugger.IsAttached;
        /// <summary>
        /// Generates autocomplete data
        /// ! Slow, long running operation
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<Autocomplete>> GenerateAutocomplete(bool debug = false, Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
        {
            AutocompleteRepo.debug = debug;
            IEnumerable<Autocomplete> companies = new List<Autocomplete>();
            IEnumerable<Autocomplete> stateCompanies = new List<Autocomplete>();
            IEnumerable<Autocomplete> authorities = new List<Autocomplete>();
            IEnumerable<Autocomplete> cities = new List<Autocomplete>();
            IEnumerable<Autocomplete> people = new List<Autocomplete>();
            IEnumerable<Autocomplete> oblasti = new List<Autocomplete>();
            IEnumerable<Autocomplete> synonyms = new List<Autocomplete>();
            IEnumerable<Autocomplete> operators = new List<Autocomplete>();
            //IEnumerable<Autocomplete> articles = new List<Autocomplete>();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = debug ? 1 : 5; // 10 bylo moc - timeoutoval nám elastic na osobách => hlidacsmluv

            Parallel.Invoke(po,
                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading cities");
                        cities = LoadCities(logOutputFunc,progressOutputFunc);
                        Consts.Logger.Info("GenerateAutocomplete Loading cities done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete Cities error ", e);
                    }
                },

                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading companies");
                        companies = LoadCompanies(logOutputFunc, progressOutputFunc);
                        Consts.Logger.Info("GenerateAutocomplete Loading companies done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete Companies error ", e);
                    }
                },
                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading state companies");
                        stateCompanies = LoadStateCompanies();
                        Consts.Logger.Info("GenerateAutocomplete Loading state companies done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete StateCompanies error ", e);
                    }
                },

                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading oblasti");
                        oblasti = LoadOblasti();
                        Consts.Logger.Info("GenerateAutocomplete Loading oblasti done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete Oblasti error ", e);
                    }
                },

                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading synonyms");
                        synonyms = LoadSynonyms();
                        Consts.Logger.Info("GenerateAutocomplete Loading synonyms done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete Synonyms error ", e);
                    }
                },

                () =>
                {
                    try
                    {
                        Consts.Logger.Info("GenerateAutocomplete Loading operators");
                        operators = LoadOperators();
                        Consts.Logger.Info("GenerateAutocomplete Loading operators done");
                    }
                    catch (Exception e)
                    {
                        Consts.Logger.Error("GenerateAutocomplete Operators error ", e);
                    }
                }
            );
            
            try
            {
                Consts.Logger.Info("GenerateAutocomplete Loading authorities");
                authorities = await LoadAuthoritiesAsync(logOutputFunc, progressOutputFunc);
                Consts.Logger.Info("GenerateAutocomplete Loading authorities done");
            }
            catch (Exception e)
            {
                Consts.Logger.Error("GenerateAutocomplete Authorities error ", e);
            }
            
            try
            {
                Consts.Logger.Info("GenerateAutocomplete Loading people");
                people = await LoadPeopleAsync(logOutputFunc, progressOutputFunc);
                Consts.Logger.Info("GenerateAutocomplete Loading people done");
            }
            catch (Exception e)
            {
                Consts.Logger.Error("GenerateAutocomplete People error ", e);
            }
            
            Consts.Logger.Info("GenerateAutocomplete done");

            return companies
            .Concat(stateCompanies)
            .Concat(authorities)
            .Concat(cities)
            .Concat(people)
            .Concat(oblasti)
            .Concat(synonyms)
            .Concat(operators)
            //.Concat(articles)
            ;
        }

        //používá se v administraci eventů pro naše politiky
        public static IEnumerable<Autocomplete> GenerateAutocompleteFirmyOnly()
        {
            string sql = $"select distinct Jmeno, ICO from Firma where LEN(ico) = 8 AND Kod_PF > 110;";
            var results = DirectDB.GetList<string, string>(sql)
                .Select(f => new Autocomplete()
                {
                    Id = f.Item2,
                    Text = f.Item1
                }).ToList();
            return results;
        }

        //firmy
        private static List<Autocomplete> LoadCompanies(Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
        {
            // Kod_PF < 110  - cokoliv co nejsou fyzické osoby, podnikatelé
            // Podnikatelé nejsou zařazeni, protože je jich poté moc a vznikají tam duplicity
            string sql = $@"select Jmeno, ICO, KrajId, status, isInRs
                             from Firma 
                            where LEN(ico) = 8 
                              AND Kod_PF > 110
                              AND (Typ is null OR Typ={(int)Firma.TypSubjektuEnum.Soukromy});";

            var lockObj = new object();
            List<Autocomplete> results = new List<Autocomplete>();

            Devmasters.Batch.Manager.DoActionForAll<Tuple<string, string, string, int?, int?>>(DirectDB.GetList<string, string, string, int?, int?>(sql).ToArray(),
                (f) =>
                {
                    Autocomplete res = null;
                    res = new Autocomplete()
                    {
                        Id = $"ico:{f.Item2}",
                        Text = f.Item1,
                        AdditionalHiddenSearchText = f.Item2,
                        Type = ("firma" + " " + Firma.StatusFull(f.Item4, true)).Trim(),
                        Description = FixKraj(f.Item3),
                        PriorityMultiplier = (f.Item4 == 1 ) ? 1f : 0.3f,
                        ImageElement = "<i class='fas fa-industry-alt'></i>",
                        Category = Autocomplete.CategoryEnum.Company
                    };

                    if (res != null)
                        lock (lockObj)
                            results.Add(res);

                    return new Devmasters.Batch.ActionOutputData();
                }, logOutputFunc, progressOutputFunc, true, prefix: "LoadSoukrFirmy autocomplete ");

            return results;
        }

        //státní firmy
        private static List<Autocomplete> LoadStateCompanies()
        {
            string sql = $@"select Jmeno, ICO, KrajId, status 
                             from Firma 
                            where IsInRS = 1 
                              AND LEN(ico) = 8 
                              AND Kod_PF > 110
                              AND Typ in ({(int)Firma.TypSubjektuEnum.PatrimStatu}
                                ,{(int)Firma.TypSubjektuEnum.PatrimStatu25perc});";
            var results = DirectDB.GetList<string, string, string, int?>(sql)
                .AsParallel()
                .Select(f => new Autocomplete()
                {
                    Id = $"ico:{f.Item2}",
                    Text = f.Item1,
                    AdditionalHiddenSearchText = f.Item2,
                    Type = ("státní firma" + " " + Firma.StatusFull(f.Item4, true)).Trim(),
                    Description = FixKraj(f.Item3),
                    PriorityMultiplier = 1.5f,
                    ImageElement = "<i class='fas fa-industry-alt'></i>",
                    KIndex = (KIndex.GetAsync(f.Item2).GetAwaiter().GetResult())?.LastKIndexLabel().ToString("G"),
                    Category = Autocomplete.CategoryEnum.StateCompany
                }).ToList();
            return results;
        }

        //úřady
        private static async Task<List<Autocomplete>> LoadAuthoritiesAsync(Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
        {
            string sql = $@"select Jmeno, ICO, KrajId , status
                             from Firma 
                            where IsInRS = 1 
                              AND LEN(ico) = 8 
                              AND Kod_PF > 110
                              AND Typ={(int)Firma.TypSubjektuEnum.Ovm};";

            var lockObj = new object();
            List<Autocomplete> results = new List<Autocomplete>();

            await Devmasters.Batch.Manager.DoActionForAllAsync<Tuple<string, string, string, int?>>(
                DirectDB.GetList<string, string, string, int?>(sql).ToArray(),
                async f =>
                {
                    string img = "<i class='fas fa-university'></i>";

                    var fi = Firmy.Get(f.Item2);
                    if (fi.Valid)
                    {
                        var o = fi.Ceo().Osoba;
                        if (o != null)
                            img = $"<img src='{o.GetPhotoUrl(false)}' />";
                    }

                    var res = new Autocomplete()
                    {
                        Id = $"ico:{f.Item2}",
                        Text = f.Item1,
                        AdditionalHiddenSearchText = f.Item2,
                        Type = "úřad",
                        Description = FixKraj(f.Item3),
                        PriorityMultiplier = 2,
                        ImageElement = img,
                        KIndex = (await KIndex.GetAsync(f.Item2))?.LastKIndexLabel().ToString("G"),
                        Category = Autocomplete.CategoryEnum.Authority
                    };

                    if (res != null)
                        lock (lockObj)
                            results.Add(res);

                    return new Devmasters.Batch.ActionOutputData();
                }, logOutputFunc, progressOutputFunc, true, prefix: "LoadUrady ");

            return results;
        }

        private static List<Autocomplete> LoadSynonyms()
        {
            string sql = $@"select text, query, type, priority, imageElement, description from AutocompleteSynonyms where active=1;";
            var results = DirectDB.GetList<string, string, string, int, string, string>(sql)
                .AsParallel()
                .Select(f => new Autocomplete()
                {
                    Id = $"{f.Item2}",
                    Text = f.Item1,
                    Type = f.Item3,
                    Description = FixKraj(f.Item6),
                    PriorityMultiplier = f.Item4 == 0 ? 1f : f.Item4,
                    ImageElement = f.Item5,
                    Category = Autocomplete.CategoryEnum.Synonym
                }).ToList();
            return results;
        }

        //obce
        private static List<Autocomplete> LoadCities(Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
        {

            var lockObj = new object();
            List<Autocomplete> results = new List<Autocomplete>();
            var obce = HlidacStatu.Repositories.FirmaRepo.Zatrideni.Subjekty(Firma.Zatrideni.SubjektyObory.Obce);
            Devmasters.Batch.Manager.DoActionForAll<Firma.Zatrideni.Item>(obce,
                (f) =>
                {

                    string img = "<i class='fas fa-city'></i>";

                    var fi = Firmy.Get(f.Ico);
                    if (fi.Valid)
                    {
                        var o = fi.Ceo().Osoba;
                        if (o != null)
                            img = $"<img src='{o.GetPhotoUrl(false)}' />";
                    }

                    var synonyms = new Autocomplete[2];
                    synonyms[0] = new Autocomplete()
                    {
                        Id = $"ico:{f.Ico}",
                        Text = f.Jmeno,
                        AdditionalHiddenSearchText = f.Ico,
                        Type = "obec",
                        Description = f.Kraj,
                        PriorityMultiplier = 2,
                        ImageElement = img,
                        Category = Autocomplete.CategoryEnum.City
                    };
                    synonyms[1] = synonyms[0].Clone();
                    string synonymText = Regex.Replace(f.Jmeno,
                        @"^(Město|Městská část|Městys|Obec|Statutární město) ?",
                        "",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    synonyms[1].Text = synonymText;

                    lock (lockObj)
                    {
                        results.Add(synonyms[0]);
                        results.Add(synonyms[1]);
                    }
                    return new Devmasters.Batch.ActionOutputData();
                }, logOutputFunc, progressOutputFunc, true, prefix: "LoadObce ");

            return results;
        }

        //lidi
        private static async Task<List<Autocomplete>> LoadPeopleAsync(Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
        {
            var excludedInfoFactImportanceLevels = new HashSet<InfoFact.ImportanceLevel>()
            {
                InfoFact.ImportanceLevel.Stat
            };
            
            List<Autocomplete> results = new List<Autocomplete>();
            using (DbEntities db = new DbEntities())
            {

                SemaphoreSlim semaphoreLock = new SemaphoreSlim(1, 1);
                await Devmasters.Batch.Manager.DoActionForAllAsync<Osoba>(db.Osoba.AsQueryable()
                    .Where(o => o.Status == (int)Osoba.StatusOsobyEnum.Politik
                        || o.Status == (int)Osoba.StatusOsobyEnum.VysokyUrednik
                        || o.Status == (int)Osoba.StatusOsobyEnum.Sponzor), async o =>
                    {
                        float priority = o.Status switch
                        {
                            (int)Osoba.StatusOsobyEnum.Politik => 2.5f,
                            (int)Osoba.StatusOsobyEnum.VysokyUrednik => 1.7f,
                            _ => 1f
                        };
                        
                        var synonyms = new Autocomplete[2];
                        synonyms[0] = new Autocomplete()
                        {
                            Id = $"osobaid:{o.NameId}",
                            Text = $"{o.Jmeno} {o.Prijmeni}{AppendTitle(o.TitulPred, o.TitulPo)}",
                            PriorityMultiplier = priority,
                            Type = o.StatusOsoby().ToNiceDisplayName(),
                            ImageElement = $"<img src='{o.GetPhotoUrl(false)}' />",
                            Description = InfoFact.RenderInfoFacts((await 
                                o.InfoFactsAsync(excludedInfoFactImportanceLevels)).ToArray(),
                                2, true, false, "", "{0}", false),
                            Category = Autocomplete.CategoryEnum.Person
                        };
 
                        synonyms[1] = synonyms[0].Clone();
                        synonyms[1].Text = $"{o.Prijmeni} {o.Jmeno}{AppendTitle(o.TitulPred, o.TitulPo)}";

                        await semaphoreLock.WaitAsync();
                        try
                        {
                            results.Add(synonyms[0]);
                            results.Add(synonyms[1]);
                        }
                        finally
                        {
                            semaphoreLock.Release();
                        }
                        
                        return new Devmasters.Batch.ActionOutputData();
                    }
                    // tady nemůže být větší paralelita, protože to pak nezvládá elasticsearch
                    , logOutputFunc, progressOutputFunc, true, prefix: "LoadPeople ", maxDegreeOfParallelism:5, monitor: new MonitoredTaskRepo.ForBatch()); 

            }
            return results;
        }

        private static string AppendTitle(string titulPred, string titulPo)
        {
            var check = (titulPred + titulPo).Trim();
            if (string.IsNullOrWhiteSpace(check))
                return "";

            var sb = new StringBuilder();
            sb.Append(" (");
            sb.Append(titulPred);
            sb.Append(" ");
            sb.Append(titulPo);
            sb.Append(")");

            return sb.ToString();
        }

        private static IEnumerable<Autocomplete> LoadOblasti()
        {
            var enumType = typeof(Smlouva.SClassification.ClassificationsTypes);
            var enumNames = Enum.GetNames(enumType);

            var oblasti = enumNames.SelectMany(e =>
            {
                var synonyms = new Autocomplete[2];
                synonyms[0] = new Autocomplete()
                {
                    Id = $"oblast:{e}",
                    Text = $"oblast: {GetNiceNameForEnum(enumType, e)}",
                    PriorityMultiplier = 3,
                    Type = "Oblast smluv - upřesnění dotazu",
                    Description = $"Oblast {GetNiceNameForEnum(enumType, e)} - smlouvy z registru smluv",
                    ImageElement = $"<img src='/content/hlidacloga/Hlidac-statu-ctverec-norm.png' />",
                    Category = Autocomplete.CategoryEnum.Oblast
                };

                synonyms[1] = synonyms[0].Clone();
                synonyms[1].Text = $"oblast:{e}";
                return synonyms;
            });

            return oblasti;
        }

        private static IEnumerable<Autocomplete> LoadOperators()
        {
            return new List<Autocomplete>()
            {
                new Autocomplete()
                {
                    Id = $"OR",
                    Text = $"OR",
                    Type = "Logické operátory",
                    Description = $"Logický operátor OR (NEBO)",
                    ImageElement = $"<img src='/content/hlidacloga/Hlidac-statu-ctverec-norm.png' />",
                    PriorityMultiplier = 3,
                    Category = Autocomplete.CategoryEnum.Operator
                },
                new Autocomplete()
                {
                    Id = $"AND",
                    Text = $"AND",
                    Type = "Logické operátory",
                    Description = $"Logický operátor AND (A)",
                    ImageElement = $"<img src='/content/hlidacloga/Hlidac-statu-ctverec-norm.png' />",
                    PriorityMultiplier = 3,
                    Category = Autocomplete.CategoryEnum.Operator
                }
            };
        }
        
        // private static IEnumerable<Autocomplete> LoadArticles()
        // {
        //     
        // }

        private static string GetNiceNameForEnum(Type enumType, string enumValue)
        {
            return ((Smlouva.SClassification.ClassificationsTypes)Enum.Parse(enumType, enumValue)).ToNiceDisplayName();
        }

        private static string FixKraj(string krajId)
        {
            if (krajId is null)
                return "neznámý kraj";

            return CZ_Nuts.Kraje.TryGetValue(krajId, out string kraj) ? kraj : krajId;
        }
    }
}