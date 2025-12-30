using Devmasters.Batch;
using Devmasters.Enums;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using Serilog;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories
{
    //migrace: tohle není repo - přestěhovat jinam => až budeme vytvářet samostatnou api
    public static class AutocompleteRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(AutocompleteRepo));


        static bool debug = System.Diagnostics.Debugger.IsAttached;

        /// <summary>
        /// Generates autocomplete data
        /// ! Slow, long running operation
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<Autocomplete>> GenerateAutocomplete(bool debug = false,
            Action<string> logOutputFunc = null, IProgressWriter progressOutputFunc = null)
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
            IEnumerable<Autocomplete> dotacePrograms = new List<Autocomplete>();
            //IEnumerable<Autocomplete> articles = new List<Autocomplete>();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism =
                debug ? 1 : 5; // 10 bylo moc - timeoutoval nám elastic na osobách => hlidacsmluv

            //todo: tohle se dá zlikvidovat
            Parallel.Invoke(po,
                () =>
                {
                    try
                    {
                        _logger.Information("GenerateAutocomplete Loading companies");
                        companies = LoadCompanies(logOutputFunc, progressOutputFunc);
                        _logger.Information("GenerateAutocomplete Loading companies done");
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "GenerateAutocomplete Companies error ");
                    }
                },
                () =>
                {
                    try
                    {
                        _logger.Information("GenerateAutocomplete Loading oblasti");
                        oblasti = LoadOblasti();
                        _logger.Information("GenerateAutocomplete Loading oblasti done");
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "GenerateAutocomplete Oblasti error ");
                    }
                },
                () =>
                {
                    try
                    {
                        _logger.Information("GenerateAutocomplete Loading synonyms");
                        synonyms = LoadSynonyms();
                        _logger.Information("GenerateAutocomplete Loading synonyms done");
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "GenerateAutocomplete Synonyms error ");
                    }
                },
                () =>
                {
                    try
                    {
                        _logger.Information("GenerateAutocomplete Loading operators");
                        operators = LoadOperators();
                        _logger.Information("GenerateAutocomplete Loading operators done");
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "GenerateAutocomplete Operators error ");
                    }
                }
            );

            try
            {
                _logger.Information("GenerateAutocomplete Loading state companies");
                stateCompanies = await LoadStateCompaniesAsync();
                _logger.Information("GenerateAutocomplete Loading state companies done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete StateCompanies error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading cities");
                cities = await LoadCitiesAsync(logOutputFunc, progressOutputFunc);
                _logger.Information("GenerateAutocomplete Loading cities done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Cities error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading dotace programs");
                dotacePrograms = await LoadDotaceProgramsAsync();
                _logger.Information("GenerateAutocomplete Loading dotace programs done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete dotace programs error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading authorities");
                authorities = await LoadAuthoritiesAsync(logOutputFunc, progressOutputFunc);
                _logger.Information("GenerateAutocomplete Loading authorities done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Authorities error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading people");
                people = await LoadPeopleAsync(logOutputFunc, progressOutputFunc);
                _logger.Information("GenerateAutocomplete Loading people done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete People error ");
            }

            _logger.Information("GenerateAutocomplete done");

            return companies
                .Concat(stateCompanies)
                .Concat(authorities)
                .Concat(cities)
                .Concat(people)
                .Concat(oblasti)
                .Concat(synonyms)
                .Concat(operators)
                .Concat(dotacePrograms);
        }

        //používá se v administraci eventů pro naše politiky
        public static IEnumerable<Autocomplete> GenerateAutocompleteFirmyOnly()
        {
            string sql = $"select distinct Jmeno, ICO from Firma where LEN(ico) = 8 AND Kod_PF > 110;";
            var results = DirectDB.Instance.GetList<string, string>(sql)
                .Select(f => new Autocomplete()
                {
                    Id = f.Item2,
                    Text = f.Item1
                }).ToList();
            return results;
        }

        private static string NormalizeJmenoForSearch(string firma)
        {
            string jmenoFirmyBezKoncovky = Firma.JmenoBezKoncovkyFull(firma, out string koncovka);
            string normalizedKoncovka = Firma.NormalizedKoncovka(koncovka);
            return $"{jmenoFirmyBezKoncovky} {normalizedKoncovka}";
        }

        //firmy
        private static List<Autocomplete> LoadCompanies(Action<string> logOutputFunc = null,
            IProgressWriter progressOutputFunc = null)
        {
            // Kod_PF < 110  - cokoliv co nejsou fyzické osoby, podnikatelé
            // Podnikatelé nejsou zařazeni, protože je jich poté moc a vznikají tam duplicity
            string sql = $@"select Jmeno, ICO, KrajId, status, isInRs
                             from Firma 
                            where LEN(ico) = 8 
                              AND Kod_PF > 110
                              AND (Typ is null OR Typ>={(int)Firma.TypSubjektuEnum.Exekutor});";

            var lockObj = new object();
            List<Autocomplete> results = new List<Autocomplete>();

            Devmasters.Batch.Manager.DoActionForAll<Tuple<string, string, string, int?, int?>>(
                DirectDB.Instance.GetList<string, string, string, int?, int?>(sql).ToArray(),
                (f) =>
                {
                    Autocomplete res = null;
                    res = new Autocomplete()
                    {
                        Id = $"ico:{f.Item2}",
                        DisplayText = f.Item1,
                        Text = NormalizeJmenoForSearch(f.Item1),
                        AdditionalHiddenSearchText = f.Item2,
                        Type = ("firma" + " " + Firma.StatusFull(f.Item4, true)).Trim(),
                        Description = FixKraj(f.Item3),
                        PriorityMultiplier = (f.Item4 == 1) ? 1f : 0.3f,
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
        private static async Task<List<Autocomplete>> LoadStateCompaniesAsync()
        {
            string sql = $@"select Jmeno, ICO, KrajId, status 
                             from Firma 
                            where IsInRS = 1 
                              AND LEN(ico) = 8 
                              AND Kod_PF > 110
                              AND Typ  = {(int)Firma.TypSubjektuEnum.PatrimStatu};";

            var items = DirectDB.Instance.GetList<string, string, string, int?>(sql);
            int maxConcurrency = 20;
            var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = items.Select(async f =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var kIndex = await KIndex.GetCachedAsync(f.Item2);

                    return new Autocomplete
                    {
                        Id = $"ico:{f.Item2}",
                        DisplayText = f.Item1,
                        Text = NormalizeJmenoForSearch(f.Item1),
                        AdditionalHiddenSearchText = f.Item2,
                        Type = ("státní firma" + " " + Firma.StatusFull(f.Item4, true)).Trim(),
                        Description = FixKraj(f.Item3),
                        PriorityMultiplier = 1.5f,
                        ImageElement = "<i class='fas fa-industry-alt'></i>",
                        KIndex = kIndex?.LastKIndexLabel().ToString("G"),
                        Category = Autocomplete.CategoryEnum.StateCompany
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        //úřady
        private static async Task<List<Autocomplete>> LoadAuthoritiesAsync(Action<string> logOutputFunc = null,
            IProgressWriter progressOutputFunc = null)
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
                DirectDB.Instance.GetList<string, string, string, int?>(sql).ToArray(),
                async f =>
                {
                    string img = "<i class='fas fa-university'></i>";

                    var fi = Firmy.Get(f.Item2);
                    if (fi.Valid)
                    {
                        var o = fi.Ceo().Osoba;
                        if (o != null)
                            img = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />";
                    }

                    var res = new Autocomplete()
                    {
                        Id = $"ico:{f.Item2}",
                        DisplayText = f.Item1,
                        Text = NormalizeJmenoForSearch(f.Item1),
                        AdditionalHiddenSearchText = f.Item2,
                        Type = "úřad",
                        Description = FixKraj(f.Item3),
                        PriorityMultiplier = 2,
                        ImageElement = img,
                        KIndex = (await KIndex.GetCachedAsync(f.Item2))?.LastKIndexLabel().ToString("G"),
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
            string sql =
                $@"select text, query, type, priority, imageElement, description from AutocompleteSynonyms where active=1;";
            var results = DirectDB.Instance.GetList<string, string, string, int, string, string>(sql)
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
        private static async Task<List<Autocomplete>> LoadCitiesAsync(Action<string> logOutputFunc = null,
            IProgressWriter progressOutputFunc = null)
        {
            var lockObj = new object();
            List<Autocomplete> results = new List<Autocomplete>();
            var obce = await FirmaCache.GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory.Obce);
            Devmasters.Batch.Manager.DoActionForAll<Firma.Zatrideni.Item>(obce,
                (f) =>
                {
                    string img = "<i class='fas fa-city'></i>";

                    var fi = Firmy.Get(f.Ico);
                    if (fi.Valid)
                    {
                        var o = fi.Ceo().Osoba;
                        if (o != null)
                            img = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />";
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
        public static async Task<List<Autocomplete>> LoadPeopleAsync(Action<string> logOutputFunc = null,
            IProgressWriter progressOutputFunc = null)
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
                            Text = $"{o.Jmeno} {o.Prijmeni}{Osoba.AppendTitle(o.TitulPred, o.TitulPo)}",
                            PriorityMultiplier = priority,
                            Type = o.StatusOsoby().ToNiceDisplayName(),
                            ImageElement = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />",
                            Description = (await
                                    o.InfoFactsAsync(excludedInfoFactImportanceLevels)).ToArray()
                                .RenderFacts(2, true, false, "", "{0}", false),
                            Category = Autocomplete.CategoryEnum.Person
                        };

                        synonyms[1] = synonyms[0].Clone();
                        synonyms[1].Text = $"{o.Prijmeni} {o.Jmeno}{Osoba.AppendTitle(o.TitulPred, o.TitulPo)}";

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
                    , logOutputFunc, progressOutputFunc, true, prefix: "LoadPeople ", maxDegreeOfParallelism: 5,
                    monitor: new MonitoredTaskRepo.ForBatch());
            }

            return results;
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

        private static async Task<IEnumerable<Autocomplete>> LoadDotaceProgramsAsync()
        {
            var programs = await DotaceRepo.GetDistinctProgramsAsync();

            var programy = programs.Select(p =>
            {
                var program = new Autocomplete()
                {
                    Id = $"program:\"{p}\"",
                    Text = $"{p}",
                    PriorityMultiplier = 1f,
                    Type = "Dotační program",
                    Description = $"Dotační program: {p}",
                    ImageElement = $"<i class='fa-duotone fa-light fa-folder-open'></i>",
                    Category = Autocomplete.CategoryEnum.DotaceProgram,
                };


                return program;
            });

            return programy;
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