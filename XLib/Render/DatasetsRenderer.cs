using HlidacStatu.Datasets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Render
{
    public static class DatasetsRenderer
    {
        public static string Render(this Registration.Template registrationTemplate, DataSet ds, string sModel, string qs = "",
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlightingData = null)
        {
            dynamic model = Newtonsoft.Json.Linq.JObject.Parse(sModel);
            return Render(registrationTemplate, ds, model, qs, highlightingData);
        }

        public static string Render(this Registration.Template registrationTemplate, DataSet ds, dynamic dmodel, string qs = "",
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlightingData = null)
        {
            string template = registrationTemplate.GetTemplateHeader(ds.DatasetId, qs) + registrationTemplate.body;
            Dictionary<string, object> globVar = new Dictionary<string, object>();
            globVar.Add("highlightingData", highlightingData);
            var xtemp = new ScribanT(template, globVar);

            var res = xtemp.Render(dmodel);
            return res;
        }

        private static async Task<string> _renderResultsInHtmlAsync<T>(this DataSearchResultBase<T> dataSearchResultBase, string query,
            Func<T, dynamic> itemToDynamicFunc, int maxToRender = int.MaxValue) where T : class
        {
            var actualNumToRender = Math.Min(maxToRender, dataSearchResultBase.Result?.Count() ?? 0);
            var content = "";
            try
            {
                var registration = await dataSearchResultBase.DataSet.RegistrationAsync(); 
                if (registration?.searchResultTemplate?.IsFullTemplate() == true)
                {
                    var model = new Registration.Template.SearchTemplateResults();
                    model.Total = dataSearchResultBase.Total;
                    model.Page = 1;
                    model.Q = query;
                    model.Result = dataSearchResultBase.Result
                        .Take(maxToRender)
                        .Select(s => itemToDynamicFunc(s))
                        .ToArray();

                    content = registration
                        .searchResultTemplate
                        .Render(dataSearchResultBase.DataSet, model, qs: query);
                }
                else
                {
                    content = "<h3>Nepodařilo se nám zobrazit vyhledané výsledky</h3>" +
                                $"<div class=\"text-center\"><a class=\"btn btn-default btn-default-new\" href=\"{dataSearchResultBase.DataSet.DatasetSearchUrl(query)}\">zobrazit všechny nalezené záznamy zde</a></div>";
                }
                if (dataSearchResultBase.Total > actualNumToRender)
                {
                    content = $"<h4>Zobrazujeme {Devmasters.Lang.CS.Plural.Get(actualNumToRender, "první výsledek", "první {0} výsledky", "prvních {0} výsledků")}</h4>"
                        + content
                        + $"<div class=\"text-center\"><a class=\"btn btn-default btn-default-new\" href=\"{dataSearchResultBase.DataSet.DatasetSearchUrl(query)}\">zobrazit všechny nalezené záznamy</a></div>";
                }
                return content;
            }
            catch (Exception)
            {
                return "<h3>Nepodařilo se nám zobrazit vyhledané výsledky</h3>" +
                    $"<div class=\"text-center\"><a class=\"btn btn-default btn-default-new\" href=\"{dataSearchResultBase.DataSet.DatasetSearchUrl(query)}\">zobrazit všechny nalezené záznamy zde</a></div>";
            }
        }

        public static Task<string> RenderResultsInHtmlAsync(this DataSearchResult dataSearchResult, string query, int maxToRender = int.MaxValue) 
            => dataSearchResult._renderResultsInHtmlAsync(query, (d) => d, maxToRender);
    }
}