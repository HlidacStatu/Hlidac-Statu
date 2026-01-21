using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Cache;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class SmlouvaRepo
    {
       
        public static async Task<byte[]> GetRawStemsFromServerAsync(string smlouvaId)
        {
            Smlouva s = await SmlouvaRepo.LoadAsync(smlouvaId);

            if (s == null)
                return null;
            
            string stemmerResponse = CallEndpoint("stemmer",
                System.Text.Json.JsonSerializer.Serialize(s),
                s.Id, TimeSpan.FromSeconds(600));
            try
            {
                var jtoken = JToken.Parse(stemmerResponse);
            }
            catch (JsonReaderException e)
            {
                _logger.Error(e, $"Stemmer returned incomplete json for {smlouvaId}");
                throw;
            }

            return Encoding.UTF8.GetBytes(stemmerResponse);
        }
        
        public static async Task<Dictionary<Smlouva.SClassification.ClassificationsTypes, decimal>> GetClassificationFromServerAsync(Smlouva s,
            bool rewriteStems = false)
        {
            var data = new Dictionary<Smlouva.SClassification.ClassificationsTypes, decimal>();
            if (s.Prilohy.Any(m => m.EnoughExtractedText) == false)
                return null;

            var stems = await SmlouvaCache.GetRawStemsAsync(s.Id, rewriteStems);
            if (string.IsNullOrEmpty(stems))
            {
                return data;
            }

            string classifierResponse = "";
            try
            {
                classifierResponse = CallEndpoint("classifier",
                    stems,
                    s.Id, TimeSpan.FromSeconds(600));
            }
            catch
            {
                //retry once with new stems
                stems = await SmlouvaCache.GetRawStemsAsync(s.Id, true);
                classifierResponse = CallEndpoint("classifier",
                    stems,
                    s.Id, TimeSpan.FromSeconds(600));
            }


            var jsonData = JObject.Parse(classifierResponse);

            if (jsonData.Children().Count() == 0)
            {
                data.Add(Smlouva.SClassification.ClassificationsTypes.OSTATNI, 0.6M);
                return data;
            }

            foreach (JProperty item in jsonData.Children())
            {
                string key = item.Name.Replace("-", "_")
                    .Replace("_generic", "_obecne"); // jsonData[i][0].Value<string>().Replace("-", "_");
                decimal prob = Util.ParseTools.ToDecimal(item.Value.ToString()) ?? 0;
                if (prob > 0)
                {
                    if (Enum.TryParse<Smlouva.SClassification.ClassificationsTypes>(key, out var typ))
                    {
                        if (!data.ContainsKey(typ))
                            data.Add(typ, prob);
                        else if (typ == Smlouva.SClassification.ClassificationsTypes.OSTATNI)
                            _logger.Warning($"Classification type lookup failure : {key}");
                    }
                    else
                    {
                        _logger.Warning("Classification type lookup failure - Invalid key " + key);
                        if (!data.ContainsKey(Smlouva.SClassification.ClassificationsTypes.OSTATNI))
                            data.Add(Smlouva.SClassification.ClassificationsTypes.OSTATNI, prob);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Gets classification
        /// </summary>
        /// <param name="idSmlouvy"></param>
        /// <returns>Classification json</returns>
        public static async Task<string> GetClassificationExplanationAsync(string idSmlouvy)
        {
            if (string.IsNullOrWhiteSpace(idSmlouvy))
                return null;

            Smlouva s = await SmlouvaRepo.LoadAsync(idSmlouvy);

            if (s == null)
                return null;
            
            var response = CallEndpoint("explain_json",
                System.Text.Json.JsonSerializer.Serialize(s),
                idSmlouvy, TimeSpan.FromSeconds(600));
            return System.Text.RegularExpressions.Regex.Unescape(response);
        }

        /// <summary>
        /// Sets new classification on smlouva. And saves data also to audit table []. 
        /// Originally created for manual override of classification.
        /// </summary>
        /// <param name="typeValues">new classification</param>
        /// <param name="username">author</param>
        public static async Task OverrideClassificationAsync(this Smlouva smlouva, int[] typeValues, string username)
        {
            if (typeValues.Length == 0)
                throw new ArgumentException($"typeValues is empty");
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException($"username is empty");

            List<Smlouva.SClassification.Classification> newClassification = new List<Smlouva.SClassification.Classification>();
            for (int i = 0; i < typeValues.Length; i++)
            {
                if (!Enum.IsDefined(typeof(Smlouva.SClassification.ClassificationsTypes), typeValues[i]))
                    throw new ArgumentException(
                        $"TypeValue [{typeValues[i]}] is not defined in {nameof(Smlouva.SClassification.ClassificationsTypes)}.");

                var classItem = new Smlouva.SClassification.Classification
                {
                    TypeValue = typeValues[i],
                    ClassifProbability = (i == 0) ? 0.8m : 0.7m
                };
                newClassification.Add(classItem);
            }

            // save to db
            await ClassificationOverrideRepo.SaveAsync(username, smlouva.Id, smlouva.Classification, newClassification);

            smlouva.Classification.TypesToProperties(newClassification.ToArray());
            smlouva.Classification.LastUpdate = DateTime.Now;
            await SmlouvaRepo.SaveAsync(smlouva);
        }

        private static string CallEndpoint(string endpoint, string content, string id, TimeSpan timeout)
        {
            var url = classificationBaseUrl() + $"/{endpoint}?doc_id={id}";
            using (Devmasters.Net.HttpClient.URLContent request = new Devmasters.Net.HttpClient.URLContent(url))
            {
                request.Method = Devmasters.Net.HttpClient.MethodEnum.POST;
                request.Tries = 3;
                request.TimeInMsBetweenTries = 5000;
                request.Timeout = (int)timeout.TotalMilliseconds;
                request.ContentType = "application/json; charset=utf-8";
                request.RequestParams.RawContent = content;
                Devmasters.Net.HttpClient.TextContentResult response = null;
                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                try
                {
                    sw.Start();
                    response = request.GetContent();
                    sw.Stop();
                    _logger.Debug($"Called classifier endpoint [{endpoint}] for {id} from {url} in {sw.ElapsedMilliseconds}ms.");

                    return response.Text;
                }
                catch (Exception e)
                {
                    sw.Stop();
                    _logger.Error(e, $"Error classifier endpoint [{endpoint}] for {id} from {url} in {sw.ElapsedMilliseconds}ms, error {e.Message}");
                    throw;
                }
            }
        }

        private static string classificationBaseUrl()
        {
            string[] baseUrl = Devmasters.Config.GetWebConfigValue("Classification.Service.Url")
                .Split(',', ';');
            //Dictionary<string, DateTime> liveEndpoints = new Dictionary<string, DateTime>();

            return baseUrl[Util.Consts.Rnd.Next(baseUrl.Length)];

        }

        public static async Task<bool> SetClassificationAsync(this Smlouva smlouva, bool rewrite = false, bool rewriteStems = false) //true if changed
        {
            if (smlouva.Prilohy == null
                || !smlouva.Prilohy.Any(m => m.EnoughExtractedText))
                return false;

            if (!rewrite && !rewriteStems && (smlouva.Classification?.LastUpdate) != null
                && ((smlouva.Classification?.GetClassif()) == null || smlouva.Classification.GetClassif().Count() != 0))
                return false;

            var types = await GetClassificationFromServerAsync(smlouva, rewriteStems);
            if (types == null)
            {
                smlouva.Classification = null;
            }
            else
            {
                Smlouva.SClassification.ClassificationsTypes[] vyjimkyClassif =
                [
                    Smlouva.SClassification.ClassificationsTypes.finance_formality,
                    Smlouva.SClassification.ClassificationsTypes.finance_repo,
                    Smlouva.SClassification.ClassificationsTypes.finance_bankovni
                ];

                var smluvniStrany = new List<Firma>();
                foreach (var strana in smlouva.Prijemce.Concat([smlouva.Platce]))
                {
                    smluvniStrany.Add(await FirmaCache.GetAsync(strana.ico));
                }
                
                if (types.Count(m => vyjimkyClassif.Contains(m.Key)) > 0
                    && smluvniStrany.Any(m => m.ESA2010?.StartsWith("12") == true) == false
                    )
                {
                    foreach (var vc in vyjimkyClassif)
                        types.Remove(vc);
                }

                Smlouva.SClassification.Classification[] newClass = types
                    .Select(m => new Smlouva.SClassification.Classification()
                    {
                        TypeValue = (int)m.Key,
                        ClassifProbability = m.Value
                    }
                    ).ToArray();
                

                var newClassRelevant = smlouva.relevantClassif(newClass);
                smlouva.Classification = new Smlouva.SClassification(newClassRelevant)
                {
                    LastUpdate = DateTime.Now
                };
            }
            return true;
        }
    }
}