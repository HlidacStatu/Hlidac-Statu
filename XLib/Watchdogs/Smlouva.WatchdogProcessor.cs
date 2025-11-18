using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Watchdogs
{
    public class SmlouvaWatchdogProcessor : IWatchdogProcessor
    {
        public WatchDog OrigWD { get; private set; }

        public SmlouvaWatchdogProcessor(WatchDog wd)
        {
            OrigWD = wd;
        }

        public async Task<DateTime> GetLatestRecAsync(DateTime toDate)
        {
            var query = "zverejneno:" + string.Format("[* TO {0}]", Repositories.Searching.Tools.ToElasticDate(toDate));
            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 0, 1, SmlouvaRepo.Searching.OrderResult.DateAddedDesc);

            if (res.IsValid == false)
                return DateTime.Now.Date.AddYears(-10);
            if (res.Total == 0)
                return DateTime.Now;
            return res.ElasticResults.Hits.First().Source.LastUpdate;
        }


        public async Task<Results> GetResultsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? maxItems = null,
            string order = null)
        {
            maxItems = maxItems ?? 30;
            string query = OrigWD.SearchTerm;
            if (fromDate.HasValue || toDate.HasValue)
            {
                query += " AND zverejneno:";
                query += string.Format("[{0} TO {1}]", Repositories.Searching.Tools.ToElasticDate(fromDate, "*"),
                    Repositories.Searching.Tools.ToElasticDate(toDate, "*"));
            }

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 0, maxItems.Value,
                (SmlouvaRepo.Searching.OrderResult)Convert.ToInt32(order ?? "4")
            );
            return new Results(res.ElasticResults.Hits.Select(m => (dynamic)m.Source), res.Total,
                query, fromDate, toDate, res.IsValid, nameof(Smlouva));
        }

        public Task<RenderedContent> RenderResultsAsync(Results data, long numOfListed = 5)
        {
            RenderedContent ret = new RenderedContent();
            if (data.Total <= (numOfListed + 2))
                numOfListed = data.Total;

            var renderH = new Render.ScribanT(HtmlTemplate.Replace("#LIMIT#", numOfListed.ToString()));
            ret.ContentHtml = renderH.Render(data);
            var renderT = new Render.ScribanT(TextTemplate.Replace("#LIMIT#", numOfListed.ToString()));
            ret.ContentText = renderT.Render(data);
            ret.ContentTitle = "Smlouvy";

            return Task.FromResult(ret);
        }

        static string HtmlTemplate = @"
    <table border='0' cellpadding='5' class='' width='100%' >
        <thead>
            <tr>
                <th></th>
                <th>Plátce</th>
                <th>Příjemci</th>
                <th>Hodnota smlouvy</th>
            </tr>
        </thead>
        <tbody>
        {{ for item in model.Items limit:#LIMIT# }}
                <tr>
                    <td>
                    {{ fn_Smlouva_GetConfidenceHtml item }}<a href='https://www.hlidacstatu.cz/Detail/{{item.Id}}?qs={{html.url_encode model.SearchQuery}}&utm_source=hlidac&utm_medium=email&utm_campaign=detail'>{{item.Id}}</a></td>
                    <td>{{item.Platce.nazev}}</td>
                    <td>{{for pp in item.Prijemce; ((fn_ShortenText pp.nazev 40) + ', '); end }}</td>
                    <td>{{ fn_FormatPrice item.CalculatedPriceWithVATinCZK html: true}}</td>
                </tr>
                <tr>
                    <td colspan='4' style='border-bottom:1px #ddd solid'>{{ fn_ShortenText item.predmet 130 }}</td>
<!-- <hr size='1' style='border-width: 1px;' /> -->
                </tr>
        {{ end }}

        {{ if (model.Total > #LIMIT#) }}
            <tr><td colspan='4' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
            <tr><td colspan='4'>                   
                <a href='https://www.hlidacstatu.cz/HledatSmlouvy?Q={{ html.url_encode model.SearchQuery }}&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
                    {{ fn_Pluralize (model.Total - #LIMIT#) '' 'Další nalezená smlouva' 'Další {0} nalezené smlouvy' 'Dalších {0} nalezených smluv' }} 
                </a>.
            </td></tr>
            <tr><td colspan='4' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
        {{ end }}
        </tbody>
    </table>
";

        static string TextTemplate = @"
        {{ for item in model.Items limit:#LIMIT# }}
------------------------------------------------------
| {{item.Platce.nazev}} -> {{for pp in item.Prijemce; ((fn_ShortenText pp.nazev 40) + ', '); end }}
-  -  -  -  -  -  -  -  -  -  -  -  -  -  -  
{{ fn_FormatPrice item.CalculatedPriceWithVATinCZK html: false }} / {{ fn_ShortenText item.predmet 50 }}

Více: https://www.hlidacstatu.cz/Detail/{{item.Id}}?utm_source=hlidac&utm_medium=emailtxt&utm_campaign=detail
======================================================

        {{ end }}

{{ if (model.Total > #LIMIT#) }}
{{ fn_Pluralize (model.Total - #LIMIT#) '' 'Další nalezená smlouva' 'Další {0} nalezené smlouvy' 'Dalších {0} nalezených smluv' }} na  https://www.hlidacstatu.cz/HledatSmlouvy?Q=@(Raw(System.Web.HttpUtility.UrlEncode(model.SpecificQuery)))&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
{{ end }}

";
    }
}