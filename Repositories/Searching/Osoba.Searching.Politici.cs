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

        static Politici()
        {
            if (PoliticiStems == null)
            {
                lock (initLock)
                {
                    if (PoliticiStems == null)
                    {
                        PoliticiStems = InitPoliticiStems();
                        // Newtonsoft.Json.JsonConvert.DeserializeObject<List<Tuple<string, string[]>>>(
                        //System.IO.File.ReadAllText(@"politiciStem.json")
                        //);
                    }
                }
            }
        }

        private class politikStem
        {
            public string NameId { get; set; }
            public string JmenoStem { get; set; }
            public string PrijmeniStem { get; set; }
        }

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
            "politiciStems",
            (o) =>
            {
                var politici = OsobaRepo.Politici.Get();
                var ret = new List<politikStem>();
                var stemCache = CalculatedStemCache.Get();
                foreach (var p in politici)
                {
                    Console.Write(".");
                    string word1 = p.Jmeno.Trim();
                    if (!stemCache.ContainsKey(word1))
                    {
                        try
                        {
                            stemCache[word1] = string.Join(" ", StemsAsync(p.Jmeno).ConfigureAwait(false).GetAwaiter().GetResult());
                        }
                        catch (Exception)
                        {
                            stemCache[word1] = p.Jmeno;
                        }

                    }
                    string word2 = p.Prijmeni.Trim();
                    if (!stemCache.ContainsKey(word2))
                    {
                        try
                        {
                            stemCache[word2] = string.Join(" ", StemsAsync(p.Prijmeni).ConfigureAwait(false).GetAwaiter().GetResult());
                        }
                        catch (Exception)
                        {
                            stemCache[word2] = p.Prijmeni;
                        }
                    }

                    ret.Add(new politikStem()
                    {
                        NameId = p.NameId,
                        JmenoStem = stemCache[word1],
                        PrijmeniStem = stemCache[word2]
                    });
                }
                CalculatedStemCache.ForceRefreshCache(stemCache);

                return ret.ToArray();
            });

        static List<string> GetPermutations(string[] words)
        {
            return GetPermutations(words, 0, words.Length - 1)
                .Select(m => string.Join(" ", m))
                .ToList();
        }
        // Recursive function to generate permutations.
            static List<string[]> GetPermutations(string[] words, int start, int end)
        {
            List<string[]> result = new List<string[]>();

            if (start == end)
            {
                // Clone the array so changes in recursion don't affect it.
                result.Add((string[])words.Clone());
            }
            else
            {
                for (int i = start; i <= end; i++)
                {
                    Swap(ref words[start], ref words[i]);
                    result.AddRange(GetPermutations(words, start + 1, end));
                    Swap(ref words[start], ref words[i]); // backtrack
                }
            }
            return result;
        }

        // Helper method to swap two elements in the array.
        static void Swap(ref string a, ref string b)
        {
            string temp = a;
            a = b;
            b = temp;
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

            foreach (var p in PoliticiStemsCache.Get())
            {
                //var cols = new string[] { p.JmenoStem.ToLower(), p.PrijmeniStem.ToLower() };
                string[] fullnamewords = (p.JmenoStem.ToLower() + " " + p.PrijmeniStem.ToLower()).Split(' ');
                var names = GetPermutations(fullnamewords);
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
        /// returns Osoba.NameId[]
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
            var debug = PoliticiStems.Where(m => m.Item1 == "jana-mrackova-vildumetzova").ToArray();
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