using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft;
using Serilog;

namespace HlidacStatu.Entities.AI
{
    public class SumarizaceJSON
    {

        [Json.Schema.Generation.AdditionalProperties(false)]
        public class FormatedResult
        {
            [Json.Schema.Generation.Required()]
            [JsonInclude]
            [JsonPropertyName("sumarizace")]
            public Item[] sumarizace { get; set; }

            static FormatedResult _sample = null;
            public static FormatedResult Sample()
            {
                if (_sample == null)
                {
                    _sample = new FormatedResult()
                    {
                        sumarizace = new Item[] {
                         new Item(){ shrnuti = "string", titulek="string" }
                        }
                    };
                }
                return _sample;
            }
        }
        private static readonly ILogger _logger = Log.ForContext<SumarizaceJSON>();
        public SumarizaceJSON() { }
        public SumarizaceJSON(IEnumerable<Item> items)
        {
            this.sumarizace = items.ToArray();
        }

        public string Version { get; set; } = "1.0.1";
        public CoreOptions usedOptions { get; set; }
        public Item[] sumarizace { get; set; }


        public string ToPlainText(string betweenParagraphs = "\n", bool useTitulek = true)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in sumarizace)
            {
                if (!string.IsNullOrWhiteSpace(item.titulek) && useTitulek)
                    sb.AppendLine(item.titulek + ":");
                sb.AppendLine(item.shrnuti);
                sb.Append(betweenParagraphs);
            }

            return sb.ToString();
        }
        public string ToHtml(
            string mainBlockTemplate = "<ul>{0}</ul>",
            string itemBlockTemplate = "<li>{0}</li>",
            string itemTitleTemplate = "<b>{0}</b>",
            string itemTitleBodyDelimiter = ":",
            string itemBodyTemplate = "<div>{0}</div>"
            )
        {
            StringBuilder sbItems = new StringBuilder();

            foreach (var item in sumarizace)
            {
                string sitem = "";
                if (!string.IsNullOrWhiteSpace(item.titulek))
                    sitem = sitem + string.Format(itemTitleTemplate, item.titulek) + itemTitleBodyDelimiter;
                sitem = sitem + string.Format(itemBodyTemplate, item.shrnuti);
                sbItems.AppendFormat(itemBlockTemplate, sitem);
            }
            var res = string.Format(mainBlockTemplate, sbItems.ToString());

            return res;
        }
        [Json.Schema.Generation.AdditionalProperties(false)]
        public class Item
        {
            [Json.Schema.Generation.Required()]
            [JsonInclude]
            [JsonPropertyName("titulek")]
            public string titulek { get; set; }
            [Json.Schema.Generation.Required()]
            [JsonInclude]
            [JsonPropertyName("shrnuti")]
            public string shrnuti { get; set; }
        }

        public static List<Item> ParseJsonResult(string json)
        {
            List<Item> res = new();

            try
            {

                JObject seriz = null;
                try
                {
                    seriz = JObject.Parse(json);

                }
                catch (Exception jex)
                {
                    //
                    string fixedJson = "";
                    try
                    {
                        var autoFixedJson = JsonRepairSharp.JsonRepair.RepairJson(json);
                        _ = JObject.Parse(autoFixedJson);
                        fixedJson = autoFixedJson;
                        goto tryParse;
                    }
                    catch (Exception)
                    {
                    }

                    //check invalid JSON
                    //{ "sumarizace": ["Zpracování osobních údajů": "Pro řádné plnění smlouvy o aditivních službách je nutné zpracovávat osobní údaje v rozsahu uvedeném v článcích 8 a 12 obchodních podmínek pro poskytované služby, jakož i ID datové schránky a telefonní číslo pro službu SMS notifikací. Objednatel, jako správce těchto údajů, je povinen zajistit jejich ochranu a dodržování právních předpisů."] }
                    string pattern = @"\{\s*""sumarizace""\s*:\s*\[\s*(?:(?<titulek>""[^""]+"")\s*:\s*(?<shrnuti>""[^""]+"")\s*,?\s*)+\s*\]\s*\}";
                    //string replacement = @"{ ""sumarizace"": [${title}: ${shrnuti}] }";

                    fixedJson = Regex.Replace(json, pattern, match =>
                    {
                        //string titles = match.Groups["titulek"].Captures[0].Value;
                        //string bods = match.Groups["shrnuti"].Captures[0].Value;

                        var titlesArray = match.Groups["titulek"].Captures;
                        var bodsArray = match.Groups["shrnuti"].Captures;

                        string output = @"{ ""sumarizace"": [";

                        for (int i = 0; i < titlesArray.Count; i++)
                        {
                            string titulekx = titlesArray[i].Value.Trim();
                            string shrnutix = bodsArray[i].Value.Trim();

                            //clean up
                            if (titulekx.Trim().ToLower() == "titul")
                                titulekx = "";
                            if (titulekx.StartsWith("\""))
                                titulekx = titulekx.Substring(1);
                            if (shrnutix.StartsWith("\""))
                                shrnutix = shrnutix.Substring(1);
                            if (titulekx.EndsWith("\""))
                                titulekx = titulekx.Substring(0, titulekx.Length - 1);
                            if (shrnutix.EndsWith("\""))
                                shrnutix = shrnutix.Substring(0, shrnutix.Length - 1);

                            output += $@"{{""titulek"":""{titulekx}"", ""shrnuti"":""{shrnutix}"" }}";
                            if (i < titlesArray.Count - 1)
                            {
                                output += ", ";
                            }
                        }

                        output += "] }";
                        return output;
                    });

                tryParse:
                    try
                    {
                        seriz = JObject.Parse(fixedJson);
                    }
                    catch (Exception e)
                    {
                        _logger.Verbose("Exception for json parsing:\n" + json, e);
                        return null;
                    }

                }

                JToken[] items = null;
                if (seriz["sumarizace"]?.HasValues == true)
                    items = seriz["sumarizace"].ToArray();
                if (items == null && seriz["summary"]?.HasValues == true)
                    items = seriz["summary"].ToArray();
                if (items == null){
                    var anySum = seriz.Children()
                        .Select(m => m as JProperty)
                        .Where(m => m != null)
                        .Where(m => 
                            m.Name.ToLower().StartsWith("sum")
                            && m.HasValues
                            && (m.Value as JArray) != null
                        );
                    if (anySum.Any())
                    {
                        items = anySum.OrderByDescending(m => ((JArray)m.Value).Count)
                            .First().Value.ToArray();
                    }
                }
                if (items?.Count() > 0)
                {
                    //JToken[] items = seriz["sumarizace"].ToArray();
                    foreach (var item in items)
                    {
                        string shrnuti = null;
                        string titulek = null;
                        if (item.Children().Count() == 2)
                        {
                            titulek = item["titulek"]?.Value<string>();
                            shrnuti = item["shrnuti"]?.Value<string>();
                            if (string.IsNullOrEmpty(titulek) && string.IsNullOrEmpty(shrnuti))
                            {
                                var values = item.Children().Select(m => ((JProperty)m).Value.ToString()).OrderBy(m => m.Length).ToArray();
                                titulek = values[0];
                                shrnuti = values[1];
                            }
                            else if (string.IsNullOrEmpty(titulek) || string.IsNullOrEmpty(shrnuti))
                                shrnuti = (titulek + " " + shrnuti).Trim();

                        }
                        else if (item.Children().Count() == 1)
                        {
                            shrnuti = string.Join(" ", item.Children().Select(m => ((JProperty)m).Value.ToString()));
                        }
                        else if (item.Children().Count() == 0 || item.Type == JTokenType.String)
                        {
                            shrnuti = item.ToString();
                        }
                        else if (item.Children().Count() > 2)
                        {
                            shrnuti = string.Join(" ", item.Children().Select(m => ((JProperty)m).Value.ToString()));
                        }
                        res.Add(new Item() { titulek = titulek, shrnuti = shrnuti });
                    }
                }
                return res;
            }
            catch (Exception e)
            {
                _logger.Error("Exception for json:\n" + json, e);
                return null;
            }
        }

    }
}
