using Devmasters;

using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace HlidacStatu.Connectors.External
{
    public class TablePolozkaClassif
    {
        static string endpoint = "http://10.10.150.203:8001";
        private static readonly ILogger _logger = Log.ForContext<TablePolozkaClassif>();

        static Dictionary<string, InDocTag> tags = null;
        static TablePolozkaClassif()
        {
            if (tags == null)
                loadtags();
        }
        static void loadtags()
        {
            using (DbEntities db = new DbEntities())
            {
                tags = db.InDocTags
                    .AsNoTracking()
                    .ToDictionary(k => k.Keyword.ToLower().RemoveAccents(), v => v);
            }
        }

        internal class ClassifResponse
        {
            public Prediction[] predictions { get; set; } = new Prediction[] { };
            internal class Prediction
            {
                public int prediction { get; set; }
                public string tag { get; set; }
                public string[] words { get; set; }
            }

        }
        private static string[] findTagsFromText(string text, string analyza)
        {
            if (text == null)
                return null;
            if (string.IsNullOrWhiteSpace(text))
                return null;
            analyza = analyza.ToLower();
            text = text.ToLower().RemoveAccents();
            List<string> res = new List<string>();
            foreach (string w in text.Split(Devmasters.Lang.CS.Stemming.Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (tags.ContainsKey(w))
                {
                    if (tags[w].Analyza.ToLower() == analyza)
                        res.Add(tags[w].Tag);
                }
            }

            return res.Distinct().ToArray();
        }

        public class Classification
        {
            internal Classification(ClassifResponse.Prediction data, string analyza, string[] tags =null)
            {
                if (data == null)
                    return;
                this.Class = ClassifNameToText(data.tag, analyza);
                this.Prediction = data.prediction;
                this.Tags = tags;
                this.UsedWordsForClassif = data.words;
            }


            public Classification() { }
            public int Prediction { get; set; }
            public string Class { get; set; }
            public string[] Tags { get; set; }
            public string[] UsedWordsForClassif { get; set; }
        }


        static object lockclassif = new object();
        static InDocJobNameDescription[] classif = null;
        private static string ClassifNameToText(string classifItem, string analyza)
        {
            if (classif == null)
            {
                lock (lockclassif)
                {
                    if (classif == null)
                    {
                        using (DbEntities db = new DbEntities())
                        {
                            classif = db.InDocJobNameDescription
                                .AsNoTracking()
                                .ToArray()
                                .Select(m => { m.Analyza = m.Analyza.ToLower(); m.Classification = m.Classification.ToLower(); return m; })
                                .ToArray();

                        }

                    }
                }
            }
            return classif
                .FirstOrDefault(m => m.Analyza == analyza.ToLower() && m.Classification == classifItem.ToLower())?
                .JobGrouped ?? classifItem;
        }

        public static Classification[] GetClassification(string text, string analyza = "IT")
        {
            var url = endpoint + $"/explain_text_json?";

            string[] tags = findTagsFromText(text,analyza);

            string normText = HlidacStatu.Util.RenderData.NormalizedTextNoStopWords(text, true, false);

            var response = callEndpoint(url, normText);
            if (string.IsNullOrEmpty(response)
                || response.Length < 5 //returns empty array []\n
                )
            {
                normText = HlidacStatu.Util.RenderData.NormalizedTextNoStopWords(text, true, true);
                response = callEndpoint(url, normText);
            }
            if (string.IsNullOrEmpty(response)
                || response.Length < 5 //returns empty array []\n
                )
                return new Classification[] { };

            var resArr = Newtonsoft.Json.JsonConvert.DeserializeObject<ClassifResponse.Prediction[]>(response);
            if (resArr == null)
                return new Classification[] { };
            if (resArr.Length == 0)
                return new Classification[] { };

            var res = resArr
                .Select(m => new Classification(m, analyza, tags))
                .OrderByDescending(m => m.Prediction)
                .ThenByDescending(m => m.UsedWordsForClassif?.Length ?? 0)
                .ThenByDescending(m => m.UsedWordsForClassif == null ? 0 : string.Concat(m.UsedWordsForClassif).Length)
                ;

            //TODO add tags

            return res.ToArray();


        }

        private static string callEndpoint(string url, string text)
        {
            using (Devmasters.Net.HttpClient.URLContent request = new Devmasters.Net.HttpClient.URLContent(url))
            {
                //request.Proxy = new Devmasters.Net.Proxies.SimpleProxy("127.0.0.1", 8888);
                request.Method = Devmasters.Net.HttpClient.MethodEnum.POST;
                request.Tries = 3;
                request.TimeInMsBetweenTries = 5000;
                request.Timeout = 60000;
                request.RequestParams.Accept = "application/json";
                request.ContentType = "application/json; charset=utf-8";
                request.RequestParams.RawContent = Newtonsoft.Json.JsonConvert.SerializeObject(text);
                Devmasters.Net.HttpClient.TextContentResult response = null;
                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                try
                {
                    sw.Start();
                    response = request.GetContent();
                    sw.Stop();

                    return response?.Text;

                }
                catch (Exception e)
                {
                    sw.Stop();
                    _logger.Error(e, $"Error classifier endpoint [{url}] for {text} in {sw.ElapsedMilliseconds}ms, error {e.Message}");
                    throw;
                }
            }

        }
    }
}
