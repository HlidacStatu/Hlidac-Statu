using Devmasters.Batch;
using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using Serilog;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Repositories.Cache;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    //migrace: tohle není repo - přestěhovat jinam => až budeme vytvářet samostatnou api
    public static class AutocompleteRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(AutocompleteRepo));
        /// <summary>
        /// Generates autocomplete data
        /// ! Slow, long running operation
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<Autocomplete>> GenerateAutocompleteAsync()
        {
            var fullAutocomplete = new List<Autocomplete>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var synonymsTask = LoadSynonymsAsync();
            var companiesTask = LoadCompaniesAsync();
            var stateCompaniesTask = LoadStateCompaniesAsync();
            var citiesTask = LoadCitiesAsync();
            var dotaceProgramsTask = LoadDotaceProgramsAsync();
            var authoritiesTask = LoadAuthoritiesAsync();
            var peopleTask = LoadPeopleAsync();
            var krajeTask = LoadKrajeAsync();
            
            //sync methods
            try
            {
                _logger.Information("GenerateAutocomplete Loading oblasti");
                fullAutocomplete.AddRange(LoadOblasti());
                _logger.Information("GenerateAutocomplete Loading oblasti done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Oblasti error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading operators");
                fullAutocomplete.AddRange(LoadOperators());
                _logger.Information("GenerateAutocomplete Loading operators done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Operators error ");
            }
            
            
            //async methods
            try
            {
                _logger.Information("GenerateAutocomplete Loading synonyms");
                fullAutocomplete.AddRange(await synonymsTask); 
                _logger.Information("GenerateAutocomplete Loading synonyms done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Synonyms error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading companies");
                fullAutocomplete.AddRange(await companiesTask);
                _logger.Information("GenerateAutocomplete Loading companies done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Companies error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading state companies");
                fullAutocomplete.AddRange(await stateCompaniesTask);
                _logger.Information("GenerateAutocomplete Loading state companies done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete StateCompanies error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading cities");
                fullAutocomplete.AddRange(await citiesTask);
                _logger.Information("GenerateAutocomplete Loading cities done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Cities error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading dotace programs");
                fullAutocomplete.AddRange(await dotaceProgramsTask);
                _logger.Information("GenerateAutocomplete Loading dotace programs done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete dotace programs error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading authorities");
                fullAutocomplete.AddRange(await authoritiesTask);
                _logger.Information("GenerateAutocomplete Loading authorities done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete Authorities error ");
            }

            try
            {
                _logger.Information("GenerateAutocomplete Loading kraje");
                fullAutocomplete.AddRange(await krajeTask);
                _logger.Information("GenerateAutocomplete Loading kraje done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete kraje error ");
            }
            
            try
            {
                _logger.Information("GenerateAutocomplete Loading people");
                fullAutocomplete.AddRange(await peopleTask);
                _logger.Information("GenerateAutocomplete Loading people done");
            }
            catch (Exception e)
            {
                _logger.Error(e, "GenerateAutocomplete People error ");
            }

            stopwatch.Stop();
            _logger.Information($"GenerateAutocomplete created {fullAutocomplete.Count} records in {stopwatch.Elapsed.Seconds} seconds.");

            return fullAutocomplete;
        }

        //používá se v administraci eventů pro naše politiky
        public static async Task<IEnumerable<Autocomplete>> GenerateAutocompleteFirmyOnlyAsync()
        {
            await using var db = new DbEntities();

            var results = await db.Firma.AsNoTracking()
                .Where(f => f.ICO.Length == 8 && f.Kod_PF > 110)
                .Select(f => new Autocomplete()
                {
                    Id = f.ICO,
                    Text = f.Jmeno
                })
                .Distinct()
                .ToListAsync();

            return results;
        }

        private static string NormalizeJmenoForSearch(string firma)
        {
            string jmenoFirmyBezKoncovky = Firma.JmenoBezKoncovkyFull(firma, out string koncovka);
            string normalizedKoncovka = Firma.NormalizedKoncovka(koncovka);
            return $"{jmenoFirmyBezKoncovky} {normalizedKoncovka}";
        }

        //firmy
        private static async Task<List<Autocomplete>> LoadCompaniesAsync()
        {
            // Kod_PF < 110  - cokoliv co nejsou fyzické osoby, podnikatelé
            // Podnikatelé nejsou zařazeni, protože je jich poté moc a vznikají tam duplicity
            await using var db = new DbEntities();
            var sqlResult = await db.Firma.AsNoTracking()
                .Where(f => f.ICO.Length == 8 && f.Kod_PF > 110)
                .Where(f => f.Typ == null || f.Typ == (int)Firma.TypSubjektuEnum.Soukromy)
                .Select(firma => new Autocomplete()
                {
                    Id = $"ico:{firma.ICO}",
                    DisplayText = firma.Jmeno,
                    Text = NormalizeJmenoForSearch(firma.Jmeno),
                    AdditionalHiddenSearchText = firma.ICO,
                    Type = ("firma" + " " + Firma.StatusFull(firma.Status, true)).Trim(),
                    Description = FixKraj(firma.KrajId),
                    PriorityMultiplier = (firma.Status == 1) ? 1f : 0.3f,
                    ImageElement = "<i class='fas fa-industry-alt'></i>",
                    Category = Autocomplete.CategoryEnum.Company
                })
                .ToListAsync();

            return sqlResult;
        }

        //státní firmy
        private static async Task<List<Autocomplete>> LoadStateCompaniesAsync()
        {
            await using var db = new DbEntities();
            var sqlResult = await db.Firma.AsNoTracking()
                .Where(f => f.IsInRS == 1 && f.ICO.Length == 8 && f.Kod_PF > 110)
                .Where(f => f.Typ == (int)Firma.TypSubjektuEnum.PatrimStatu)
                .Select(f => new
                {
                    f.Jmeno,
                    f.ICO,
                    f.KrajId,
                    f.Status,
                })
                .ToListAsync();

            var bag = new ConcurrentBag<Autocomplete>();
            await Parallel.ForEachAsync(sqlResult, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (firma, ct) =>
                {
                    var kIndex = await KIndex.GetCachedAsync(firma.ICO);

                    bag.Add(new Autocomplete
                    {
                        Id = $"ico:{firma.ICO}",
                        DisplayText = firma.Jmeno,
                        Text = NormalizeJmenoForSearch(firma.Jmeno),
                        AdditionalHiddenSearchText = firma.ICO,
                        Type = ("státní firma" + " " + Firma.StatusFull(firma.Status, true)).Trim(),
                        Description = FixKraj(firma.KrajId),
                        PriorityMultiplier = 1.5f,
                        ImageElement = "<i class='fas fa-industry-alt'></i>",
                        KIndex = kIndex?.LastKIndexLabel().ToString("G"),
                        Category = Autocomplete.CategoryEnum.StateCompany
                    });
                });

            return bag.ToList();
        }

        //úřady
        private static async Task<List<Autocomplete>> LoadAuthoritiesAsync(Action<string> logOutputFunc = null,
            IProgressWriter progressOutputFunc = null)
        {
            await using var db = new DbEntities();
            var sqlResult = await db.Firma.AsNoTracking()
                .Where(f => f.IsInRS == 1 && f.ICO.Length == 8 && f.Kod_PF > 110)
                .Where(f => f.Typ == (int)Firma.TypSubjektuEnum.Ovm)
                .Select(f => new
                {
                    f.Jmeno,
                    f.ICO,
                    f.KrajId,
                    f.Status,
                })
                .ToListAsync();

            var bag = new ConcurrentBag<Autocomplete>();
            await Parallel.ForEachAsync(sqlResult, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (firma, ct) =>
                {
                    string img = "<i class='fas fa-university'></i>";

                    var fi = Firmy.Get(firma.ICO);
                    if (fi?.Valid == true)
                    {
                        var o = (await fi.CurrentCeoAsync()).Osoba;
                        if (o != null)
                            img = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />";
                    }

                    bag.Add(new Autocomplete
                    {
                        Id = $"ico:{firma.ICO}",
                        DisplayText = firma.Jmeno,
                        Text = NormalizeJmenoForSearch(firma.Jmeno),
                        AdditionalHiddenSearchText = firma.ICO,
                        Type = "úřad",
                        Description = FixKraj(firma.KrajId),
                        PriorityMultiplier = 2,
                        ImageElement = img,
                        KIndex = (await KIndex.GetCachedAsync(firma.ICO))?.LastKIndexLabel().ToString("G"),
                        Category = Autocomplete.CategoryEnum.Authority
                    });
                });

            return bag.ToList();
        }

        private static async Task<List<Autocomplete>> LoadSynonymsAsync()
        {
            await using var db = new DbEntities();
            var sqlResult = await db.AutocompleteSynonyms.AsNoTracking()
                .Where(f => f.Active == 1)
                .Select(f => new Autocomplete()
                {
                    Id = $"{f.Query}",
                    Text = f.Text,
                    Type = f.Type,
                    Description = FixKraj(f.Description),
                    PriorityMultiplier = f.Priority == 0 ? 1f : f.Priority,
                    ImageElement = f.ImageElement,
                    Category = Autocomplete.CategoryEnum.Synonym
                })
                .ToListAsync();

            return sqlResult;
        }

        //obce
        private static async Task<List<Autocomplete>> LoadCitiesAsync()
        {
            var obce = await FirmaCache.GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory.Obce);
            var bag = new ConcurrentBag<Autocomplete>();
            await Parallel.ForEachAsync(obce, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (firma, ct) =>
            {
                string img = "<i class='fas fa-city'></i>";

                var fi = Firmy.Get(firma.Ico);
                if (fi?.Valid == true)
                {
                    var o = (await fi.CurrentCeoAsync()).Osoba;
                    if (o != null)
                        img = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />";
                }

                var synonyms = new Autocomplete[2];
                synonyms[0] = new Autocomplete()
                {
                    Id = $"ico:{fi.ICO}",
                    Text = fi.Jmeno,
                    AdditionalHiddenSearchText = fi.ICO,
                    Type = "obec",
                    Description = firma.Kraj,
                    PriorityMultiplier = 2,
                    ImageElement = img,
                    Category = Autocomplete.CategoryEnum.City
                };
                synonyms[1] = synonyms[0].Clone();
                string synonymText = Regex.Replace(firma.Jmeno,
                    @"^(Město|Městská část|Městys|Obec|Statutární město) ?",
                    "",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                synonyms[1].Text = synonymText;

                bag.Add(synonyms[0]);
                bag.Add(synonyms[1]);
            });

            return bag.ToList();
        }

        //kraje
        private static async Task<List<Autocomplete>> LoadKrajeAsync()
        {
            var kraje = await FirmaCache.GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory.Kraje_Praha);
            var bag = new ConcurrentBag<Autocomplete>();
            await Parallel.ForEachAsync(kraje, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (firma, ct) =>
            {
                string img = "<i class='fas fa-city'></i>";

                var fi = Firmy.Get(firma.Ico);
                if (fi?.Valid == true)
                {
                    var o = (await fi.CurrentCeoAsync()).Osoba;
                    if (o != null)
                        img = $"<img src='{o.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />";
                }

                var acKraje = new Autocomplete()
                {
                    Id = $"ico:{fi.ICO}",
                    Text = fi.Jmeno,
                    AdditionalHiddenSearchText = fi.ICO,
                    Type = "kraj",
                    Description = firma.Kraj,
                    PriorityMultiplier = 2,
                    ImageElement = img,
                    Category = Autocomplete.CategoryEnum.City
                };

                bag.Add(acKraje);
            });

            return bag.ToList();
        }


        private static float GetPersonPriority(int osobaStatus)
        {
            return osobaStatus switch
            {
                (int)Osoba.StatusOsobyEnum.Politik => 2.5f,
                (int)Osoba.StatusOsobyEnum.VysokyUrednik => 1.7f,
                _ => 1f
            };
        }

        //lidi
        public static async Task<List<Autocomplete>> LoadPeopleAsync()
        {
            var excludedInfoFactImportanceLevels = new HashSet<Fact.ImportanceLevel>()
            {
                Fact.ImportanceLevel.Stat
            };


            await using var db = new DbEntities();
            var sqlResult = await db.Osoba.AsNoTracking()
                .Where(o => o.Status == (int)Osoba.StatusOsobyEnum.Politik
                            || o.Status == (int)Osoba.StatusOsobyEnum.VysokyUrednik
                            || o.Status == (int)Osoba.StatusOsobyEnum.Sponzor)
                .ToListAsync();

            var bag = new ConcurrentBag<Autocomplete>();
            //parallelism is lowered due to the infofacts possible issues during high usage
            await Parallel.ForEachAsync(sqlResult, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (osoba, ct) =>
                {
                    var ac = new Autocomplete()
                    {
                        Id = $"osobaid:{osoba.NameId}",
                        Text = $"{osoba.Jmeno} {osoba.Prijmeni}{Osoba.AppendTitle(osoba.TitulPred, osoba.TitulPo)}",
                        PriorityMultiplier = GetPersonPriority(osoba.Status),
                        Type = osoba.StatusOsoby().ToNiceDisplayName(),
                        ImageElement =
                            $"<img src='{osoba.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground, false)}' />",
                        Description = (await osoba.InfoFactsAsync(excludedInfoFactImportanceLevels)).ToArray()
                            .RenderFacts(2, true, false, "", "{0}", false),
                        Category = Autocomplete.CategoryEnum.Person
                    };

                    var synonyme = ac.Clone();
                    synonyme.Text =
                        $"{osoba.Prijmeni} {osoba.Jmeno}{Osoba.AppendTitle(osoba.TitulPred, osoba.TitulPo)}";

                    bag.Add(ac);
                    bag.Add(synonyme);
                });

            return bag.ToList();
        }


        private static List<Autocomplete> LoadOblasti()
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

            return oblasti.ToList();
        }

        private static async Task<List<Autocomplete>> LoadDotaceProgramsAsync()
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

            return programy.ToList();
        }

        private static List<Autocomplete> LoadOperators()
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