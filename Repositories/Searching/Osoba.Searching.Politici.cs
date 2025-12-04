using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Searching
{
    public class Politici
    {
        private static Random Rnd = new Random();

        private static List<Tuple<string, string[]>> _politiciStems = null;
        private static readonly SemaphoreSlim _initSemaphore = new (1);

        
        private static readonly IFusionCache _permanentCache =
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(Politici));

        private static ValueTask<PolitikStem[]> GetPoliticiStemsAsync() =>
            _permanentCache.GetOrSetAsync($"_politiciStems_",
                _ => RecalculatePoliticiStemsAsync(),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(1), TimeSpan.FromDays(10))
            );
        

        internal class PolitikStem
        {
            public string NameId { get; set; }
            public string[] Stems { get; set; }
        }

        //L1 - 12 h
        //L2 - 10 let

        public static Devmasters.Cache.AWS_S3.Cache<Dictionary<string, string>> CalculatedStemCache { get; set; } =
            new Devmasters.Cache.AWS_S3.Cache<Dictionary<string, string>>(
            new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
            Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero,
            "politiciSCalculatedStems",
            (o) =>
            {
                return new Dictionary<string, string>();
            });

        internal static async Task<PolitikStem[]> RecalculatePoliticiStemsAsync()
        {
            var politici = OsobaRepo.Politici.Get();
            var ret = new List<PolitikStem>();
            var stemCache = CalculatedStemCache.Get();
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
            CalculatedStemCache.ForceRefreshCache(stemCache);

            return ret.ToArray();
        }

        static async Task<List<Tuple<string, string[]>>> InitPoliticiStemsAsync()
        {
            if(_politiciStems != null)
                return _politiciStems;
            
            await _initSemaphore.WaitAsync();
            try
            {
                string[] prefixes = ("pan kolega poslanec předseda místopředseda prezident premiér "
                                     + "paní slečna kolegyně poslankyně předsedkyně místopředsedkyně prezidentka premiérka ")
                    .Split(' ');
                string[] blacklist = { "poslanec celý" };
                string[] whitelist = { };

                var politiciStems = new List<Tuple<string, string[]>>();

                foreach (var p in await GetPoliticiStemsAsync())
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
            finally
            {
                _initSemaphore.Release();
            }
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
                var res = await wc.PostAsync(classificationBaseUrl() + "/text_stemmer_ngrams?ngrams=" + ngramLength, data).ConfigureAwait(false);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(await res.Content.ReadAsStringAsync().ConfigureAwait(false));

            }
            catch (Exception e)
            {

                if (tries < 6)
                {
                    System.Threading.Thread.Sleep(20 * tries);
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
            Dictionary<string, string> compareVyjimky = new Dictionary<string, string>() {
                { "jan","jana" },
                { "jana","jan" }
            };

            var stopw = new Devmasters.DT.StopWatchEx();
            stopw.Start();
            string[] sText = await StemsAsync(text);
            stopw.Stop();
            //Console.WriteLine($"stemmer {stopw.ExactElapsedMiliseconds} ");
            stopw.Restart();
            List<string> found = new List<string>();
            var politiciStems = await InitPoliticiStemsAsync();
            var debug = politiciStems.Where(m => m.Item1 == "jozef-sikela").ToArray();
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
                            same = same & (compareVyjimky[stl].Equals(politik[j], StringComparison.InvariantCultureIgnoreCase));
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