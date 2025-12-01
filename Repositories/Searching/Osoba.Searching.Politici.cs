using Devmasters.Collections;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Searching
{
    public class Politici
    {
        public static Random Rnd = new Random();

        public static List<Tuple<string, string[]>> PoliticiStems = null;

        static object initLock = new object();
        private static readonly ILogger _logger = Log.ForContext(typeof(Politici));


        static Politici()
        {
            if (PoliticiStems == null)
            {
                lock (initLock)
                {
                    if (PoliticiStems == null)
                    {
                        _logger.Warning("InitPoliticiStems start  ");
                        PoliticiStems = InitPoliticiStems();
                        _logger.Warning("InitPoliticiStems end ");
                        // Newtonsoft.Json.JsonConvert.DeserializeObject<List<Tuple<string, string[]>>>(
                        //System.IO.File.ReadAllText(@"politiciStem.json")
                        //);
                    }
                }
            }
        }

        internal class politikStem
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

        private static Devmasters.Cache.AWS_S3.AutoUpdatebleCache<politikStem[]> PoliticiStemsCache =
            new Devmasters.Cache.AWS_S3.AutoUpdatebleCache<politikStem[]>(
            new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
            Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.FromDays(5),
            "politiciStems_v2",
            (o) =>
            {
                return RecalculatePoliticiStems();
            });

        internal static politikStem[] RecalculatePoliticiStems()
        {
            var politici = OsobaRepo.Politici.Get();
            var ret = new List<politikStem>();
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
                        stems1 = StemsAsync(word1).ConfigureAwait(false).GetAwaiter().GetResult();
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
                        stems2 = StemsAsync(word2).ConfigureAwait(false).GetAwaiter().GetResult();
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


                ret.Add(new politikStem()
                {
                    NameId = p.NameId,
                    Stems = stems1
                });
                if (addOpposite)
                {
                    ret.Add(new politikStem()
                    {
                        NameId = p.NameId,
                        Stems = stems2
                    });
                }
                //if (p.Jmeno != stemCache[word1] || p.Prijmeni != stemCache[word2])
                //{

                //    ret.Add(new politikStem()
                //    {
                //        NameId = p.NameId,
                //        JmenoStem = p.Jmeno,
                //        PrijmeniStem = p.Prijmeni
                //    });
                //}
            }
            CalculatedStemCache.ForceRefreshCache(stemCache);

            return ret.ToArray();
        }

        static List<Tuple<string, string[]>> InitPoliticiStems()
        {
            HashSet<string> slova = new HashSet<string>();
            string[] prefixes = ("pan kolega poslanec předseda místopředseda prezident premiér "
                                 + "paní slečna kolegyně poslankyně předsedkyně místopředsedkyně prezidentka premiérka ")
                .Split(' ');
            string[] blacklist = { "poslanec celý" };
            string[] whitelist = { };

            var politiciStems = new List<Tuple<string, string[]>>();

            var path = Connectors.Init.WebAppDataPath;
            if (string.IsNullOrWhiteSpace(path))
                path = Devmasters.IO.IOTools.GetExecutingDirectoryName(true);

            foreach (var s in Repositories.StaticData.CzechDictCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries))
            {
                slova.Add(s);
            }
            if (false && System.Diagnostics.Debugger.IsAttached)
            {
                politikStem[] newStems = RecalculatePoliticiStems();
                PoliticiStemsCache.ForceRefreshCache(newStems);
            }

            foreach (var p in PoliticiStemsCache.Get())
            {
                //var cols = new string[] { p.JmenoStem.ToLower(), p.PrijmeniStem.ToLower() };
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
            //Dictionary<string, DateTime> liveEndpoints = new Dictionary<string, DateTime>();
            var url = baseUrl[Rnd.Next(baseUrl.Length)];

            //if (System.Diagnostics.Debugger.IsAttached)
            //    url = "http://localhost:8080";


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
            var debug = PoliticiStems.Where(m => m.Item1 == "jozef-sikela").ToArray();
            foreach (var kv in PoliticiStems)
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
            //Console.WriteLine($"location {stopw.ExactElapsedMiliseconds} ");
            return found.ToArray();
        }
    }
}