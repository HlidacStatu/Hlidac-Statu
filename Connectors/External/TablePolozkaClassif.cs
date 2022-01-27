using System;
using System.Linq;

namespace HlidacStatu.Connectors.External
{
    public class TablePolozkaClassif
    {
        static string endpoint = "http://10.10.150.203:8001";


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
        public class Classification
        {
            internal Classification(ClassifResponse.Prediction data)
            {
                if (data == null)
                    return;
                this.Class = data.tag;
                this.Prediction = data.prediction;
                this.UsedWordsForClassif = data.words;
            }
            public Classification() { }
            public int Prediction { get; set; }
            public string Class { get; set; }
            public string[] Tags { get; set; }
            public string[] UsedWordsForClassif { get; set; }
        }


        public static Classification[] GetClassification(string text, string analyza = "IT")
        {
            var url = endpoint + $"/explain_text_json?";
            var response = callEndpoint(url, text);
            if (string.IsNullOrEmpty(response)
                || response.Length < 5 //returns empty array []\n
                )
            {

                var accentedCharacters = "àèìòùÀÈÌÒÙáéíóúýÁÉÍÓÚÝâêîôûÂÊÎÔÛãñõÃÑÕäëïöüÿÄËÏÖÜŸçÇßØøÅåÆæœčČšŠřŘžŽťŤňŇďĎĺĹ";
                string normText = System.Text.RegularExpressions.Regex.Replace(text, "[^a-zA-Z0-9" + accentedCharacters + "'-]{1}", " ");
                normText = System.Text.RegularExpressions.Regex.Replace(normText, @"\s[a-zA-Z0-9" + accentedCharacters + @"-]{1}\s", " ");
                normText = Devmasters.TextUtil.ReplaceDuplicates(normText, " ");
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
                .Select(m => new Classification(m))
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
                    Util.Consts.Logger.Error($"Error classifier endpoint [{url}] for {text} in {sw.ElapsedMilliseconds}ms, error {e.Message}", e);
                    throw;
                }
            }

        }
    }
}
