using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Searching
{
    public class Politici
    {
        private static Random Rnd = new Random();
        
        private static readonly object _memoryCachelock = new object();
        private static IFusionCache _memoryCache;
        private static IFusionCache MemoryCache
        {
            get
            {
                if (_memoryCache == null)
                {
                    lock (_memoryCachelock)
                    {
                        _memoryCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                            CacheFactory.CacheType.L1Default,
                            nameof(Politici));
                    }
                }

                return _memoryCache;
            }
        }
        
        private static readonly object _permanentCacheLock = new();
        private static IFusionCache _permanentCache;
        private static IFusionCache PermanentCache
        {
            get
            {
                if (_permanentCache == null)
                {
                    lock (_permanentCacheLock)
                    {
                        _permanentCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                            CacheFactory.CacheType.PermanentStore,
                            nameof(Politici));
                    }
                }

                return _permanentCache;
            }
        }

        //Tady byly cache původně blbě a nedocházelo ke správným opravám dat, pokud aplikace běžela déle než 5 dní
        
        // 1 hodinu v paměti, 10 let životnost, k naplnění dojde vždy při spuštění GetCachedPoliticiStemsAsync()
        private static Dictionary<string, string> GetOrSetCalculatedStemsCache(Dictionary<string, string> data = null)
        {
            string cacheKey = $"_CalculatedPoliticiStems_";
            if (data is null || !data.Any())
            {
                return PermanentCache.GetOrDefault(cacheKey, new Dictionary<string, string>());
            }
            
            PermanentCache.Set(cacheKey, data, options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(1), TimeSpan.FromDays(10 * 365));
                options.FactoryHardTimeout = TimeSpan.FromHours(1);
            });
            return data;
        }
        
        // 1 hodinu v paměti, 4 dni živnotnost (aby se spustila rekalkulace při dalším příp. startu), 
        // 16 dní failsafe
        private static ValueTask<PolitikStem[]> GetCachedPoliticiStemsAsync() =>
            PermanentCache.GetOrSetAsync($"_politiciStems_",
                _ => RecalculatePoliticiStemsAsync(),
                options =>
                {
                    options.ModifyEntryOptionsDuration(TimeSpan.FromHours(1), TimeSpan.FromDays(4));
                    options.FactoryHardTimeout = TimeSpan.FromHours(1);
                });
        
        //5 dní v paměti, pak rekalk => spustí rekalk GetCachedPoliticiStemsAsync()
        private static ValueTask<List<Tuple<string, string[]>>> PoliticiStemsCachedAsync() =>
            MemoryCache.GetOrSetAsync($"_politiciStemsInMem_",
                _ => InitPoliticiStemsAsync(),
                options =>
                {
                    options.ModifyEntryOptionsDuration(TimeSpan.FromDays(5));
                    options.FactoryHardTimeout = TimeSpan.FromHours(1);
                });


        internal class PolitikStem
        {
            public string NameId { get; set; }
            public string[] Stems { get; set; }
        }
        

        internal static async Task<PolitikStem[]> RecalculatePoliticiStemsAsync()
        {
            var politici = OsobaRepo.Politici.Get();
            var ret = new List<PolitikStem>();
            var stemCache = GetOrSetCalculatedStemsCache();
            foreach (var p in politici)
            {
                Console.Write(".");
                string word1 = (p.Jmeno + " " + p.Prijmeni).Trim();
                string[] stems1 = null;
                if (!stemCache.ContainsKey(word1))
                {
                    try
                    {
                        stems1 = await StemsAsync(word1);
                        stemCache[word1] = string.Join(" ", stems1);
                    }
                    catch (Exception)
                    {
                        stemCache[word1] = word1;
                    }
                }

                stems1 = stemCache[word1].Split(' ');

                bool addOpposite = false;
                string[] stems2 = null;
                string word2 = (p.Prijmeni + " " + p.Jmeno).Trim();
                if (!stemCache.ContainsKey(word2))
                {
                    try
                    {
                        stems2 = await StemsAsync(word2);
                        stemCache[word2] = string.Join(" ", stems2);

                        //jsou rozdilne stemy pro opacne jmeno a prijmeni?
                        addOpposite = stems2.Except(stems1).Any();
                    }
                    catch (Exception)
                    {
                        stemCache[word2] = word2;
                    }
                }

                stems2 = stemCache[word2].Split(' ');


                ret.Add(new PolitikStem()
                {
                    NameId = p.NameId,
                    Stems = stems1
                });
                if (addOpposite)
                {
                    ret.Add(new PolitikStem()
                    {
                        NameId = p.NameId,
                        Stems = stems2
                    });
                }
            }
            
            GetOrSetCalculatedStemsCache(stemCache);

            return ret.ToArray();
        }

        private static async Task<List<Tuple<string, string[]>>> InitPoliticiStemsAsync()
        {
            string[] prefixes = ("pan kolega poslanec předseda místopředseda prezident premiér "
                                 + "paní slečna kolegyně poslankyně předsedkyně místopředsedkyně prezidentka premiérka ")
                .Split(' ');
            string[] blacklist = { "poslanec celý" };
            string[] whitelist = { };

            var politiciStems = new List<Tuple<string, string[]>>();

            foreach (var p in await GetCachedPoliticiStemsAsync())
            {
                string[] nameStems = p.Stems;
                var names = HlidacStatu.Util.TextTools.GetPermutations(nameStems);
                var key = p.NameId;


                foreach (var n in names)
                {
                    var fname = (n).Split(' ');
                    if (!blacklist.Contains(n) && fname.Length > 1)
                        politiciStems.Add(new Tuple<string, string[]>(key, fname));

                    foreach (var pref in prefixes)
                    {
                        if (!blacklist.Contains(pref + " " + n))
                        {
                            var nn = (pref + " " + n).Split(' ');
                            if (nn.Length > 1)
                                politiciStems.Add(new Tuple<string, string[]>(key, nn));
                        }
                    }
                }
            }

            return politiciStems;
        }

        public static async Task<string[]> StemsAsync(string text, int ngramLength = 1)
        {
            int tries = 0;
            start:
            try
            {
                tries++;
                using var wc = new System.Net.Http.HttpClient();
                var data = System.Net.Http.Json.JsonContent.Create(text);
                var res = await wc
                    .PostAsync(classificationBaseUrl() + "/text_stemmer_ngrams?ngrams=" + ngramLength, data)
                    .ConfigureAwait(false);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(await res.Content.ReadAsStringAsync()
                    .ConfigureAwait(false));
            }
            catch (Exception e)
            {
                if (tries < 6)
                {
                    await Task.Delay(20 * tries);
                    goto start;
                }
                else
                    throw;
            }
        }

        private static string classificationBaseUrl()
        {
            string[] baseUrl = Devmasters.Config.GetWebConfigValue("Classification.Service.Url")
                .Split(',', ';');

            var url = baseUrl[Rnd.Next(baseUrl.Length)];
            return url;
        }

        /// <summary>
        /// returns Osoba.NameId
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static async Task<string[]> FindCitationsAsync(string text)
        {
            Dictionary<string, string> compareVyjimky = new Dictionary<string, string>()
            {
                { "jan", "jana" },
                { "jana", "jan" }
            };

            var stopw = new Devmasters.DT.StopWatchEx();
            stopw.Start();
            string[] sText = await StemsAsync(text);
            stopw.Stop();
            //Console.WriteLine($"stemmer {stopw.ExactElapsedMiliseconds} ");
            stopw.Restart();
            List<string> found = new List<string>();
            var politiciStems = await PoliticiStemsCachedAsync();
            foreach (var kv in politiciStems)
            {
                string zkratka = kv.Item1;
                string[] politik = kv.Item2;

                for (int i = 0; i < sText.Length - (politik.Length - 1); i++)
                {
                    bool same = true;
                    for (int j = 0; j < politik.Length; j++)
                    {
                        string stl = sText[i + j].ToLower();
                        if (sText[i + j] == politik[j])
                            same = same & true;
                        else if (compareVyjimky.ContainsKey(stl))
                        {
                            same = same & (compareVyjimky[stl]
                                .Equals(politik[j], StringComparison.InvariantCultureIgnoreCase));
                        }
                        else
                        {
                            same = false;
                        }

                        if (same == false)
                            break;
                    }

                    if (same)
                    {
                        if (!found.Contains(zkratka))
                            found.Add(zkratka);
                        break;
                    }
                }
            }

            stopw.Stop();
            return found.ToArray();
        }
    }
}