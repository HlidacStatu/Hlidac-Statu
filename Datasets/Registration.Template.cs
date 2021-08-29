using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Datasets
{
    public partial class Registration
    {
        public partial class Template
        {
            public string header { get; set; } = null;
            public string body { get; set; } = null;
            public string footer { get; set; } = null;
            public string title { get; set; } = null;
            public string[] properties { get; set; } = null;

            public string ToPageContent()
            {
                if (!string.IsNullOrEmpty(body))
                    return (header ?? "") + body + (footer ?? "");
                else
                    return null;
            }

            public bool IsFullTemplate()
            {
                return !string.IsNullOrEmpty(body);
            }
            public bool IsNewTemplate()
            {
                return (body ?? "").Contains("{{");
            }



            public string GetTemplateHeader(string datasetId, string qs)
            {
                string template = "{{ \n" +
                    " qs = \"" + System.Net.WebUtility.UrlEncode(qs) + "\""
                    + "\n"
                    + "func fn_DatasetItemUrl" + "\n"
                    + $"    ret ('https://www.hlidacstatu.cz/data/Detail/{datasetId}/' + $0 + '?qs=' + qs )"
                    + "\n"
                    + "end}}"
                    + "\n"
                    //+ "<!-- This is var1: `{{highlightingData}}` -->"
                    + "\n\n";

                return template;
            }

            public List<string> GetTemplateErrors()
            {
                string template = GetTemplateHeader("any", "") + body;
                var xTemp = Scriban.Template.Parse(template);
                if (xTemp.HasErrors)
                {
                    return xTemp
                        .Messages
                        .Select(m => m.ToString())
                        .ToList();
                }
                return new List<string>();
            }

            public class SearchTemplateResults
            {
                public long Total { get; set; }
                public IEnumerable<dynamic> Result { get; set; }
                public string Q { get; set; }
                public int Page { get; set; }
            }


        }
    }
}
