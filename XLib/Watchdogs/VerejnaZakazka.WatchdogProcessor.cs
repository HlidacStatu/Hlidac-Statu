using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Watchdogs
{
    public class VerejnaZakazkaWatchdogProcessor : IWatchdogProcessor
    {
        public WatchDog OrigWD { get; private set; }

        public VerejnaZakazkaWatchdogProcessor(WatchDog wd)
        {
            OrigWD = wd;
        }

        public async Task<DateTime> GetLatestRecAsync(DateTime toDate)
        {
            var query = "posledniZmena:" +
                        string.Format("[* TO {0}]", Repositories.Searching.Tools.ToElasticDate(toDate));
            var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, null, 0, 1,
                ((int)Repositories.Searching.VerejnaZakazkaSearchData.VZOrderResult.LastUpdate).ToString());

            if (res.IsValid == false)
                return DateTime.Now.Date.AddYears(-10);
            if (res.Total == 0)
                return DateTime.Now;
            return res.ElasticResults.Hits.First().Source.PosledniZmena ??
                   res.ElasticResults.Hits.First().Source.LastUpdated.Value;
        }


        public async Task<Results> GetResultsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? maxItems = null,
            string order = null)
        {
            maxItems = maxItems ?? 30;
            string query = OrigWD.SearchTerm;
            if (fromDate.HasValue || toDate.HasValue)
            {
                query += " AND posledniZmena:";
                query += string.Format("[{0} TO {1}]", Repositories.Searching.Tools.ToElasticDate(fromDate, "*"),
                    Repositories.Searching.Tools.ToElasticDate(toDate, "*"));
            }

            var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, null, 0, 50,
                order == null
                    ? ((int)Repositories.Searching.VerejnaZakazkaSearchData.VZOrderResult.DateAddedDesc).ToString()
                    : order,
                OrigWD.FocusId == 1);

            return new Results(res.ElasticResults.Hits.Select(m => (dynamic)m.Source), res.Total,
                query, fromDate, toDate, res.IsValid, nameof(VerejnaZakazka));
        }

        public Task<RenderedContent> RenderResultsAsync(Results data, long numOfListed = 5)
        {
            RenderedContent ret = new RenderedContent();
            List<RenderedContent> items = new List<RenderedContent>();
            if (data.Total <= (numOfListed + 2))
                numOfListed = data.Total;

            var renderH = new Render.ScribanT(HtmlTemplate.Replace("#LIMIT#", numOfListed.ToString()));
            ret.ContentHtml = renderH.Render(data);
            var renderT = new Render.ScribanT(TextTemplate.Replace("#LIMIT#", numOfListed.ToString()));
            ret.ContentText = renderT.Render(data);
            ret.ContentTitle = "Veřejné zakázky";

            return Task.FromResult(ret);
        }

        static string HtmlTemplate = @"


    <table border='0' cellpadding='4' width='100%'>
        <thead>
            <tr>
                <th>Zakázka</th>
                <th>Poslední změna</th>
                <th>Lhůta pro nabídky</th>
                <th>Zadavatel</th>
                <th>Název</th>
                <th>Cena</th>
            </tr>
        </thead>
        <tbody>
        {{ for item in model.Items limit:5 }}
            <tr>
                <td style='white-space: nowrap;'>
                    <a href='https://www.hlidacstatu.cz/VerejneZakazky/Zakazka/{{ item.Id }}?qs={{html.url_encode model.SearchQuery}}&utm_source=hlidac&utm_medium=email&utm_campaign=detail'>{{item.EvidencniCisloZakazky}}</a>
                </td>
                <td>
                    {{ if (item.DatumUverejneni != null) ; fn_FormatDate item.DatumUverejneni 'dd.MM.yyyy' ; end }}
                </td>
                <td>
                    {{ if (item.LhutaDoruceni == null) 
                        'neuvedena'  
                        else 
                        fn_FormatDate item.LhutaDoruceni 'dd.MM.yyyy' 
                    end }}
                </td>
                <td>
                    <span>{{ if (item.Zadavatel == null)  
                        'neuveden'  
                        else 
                          item.Zadavatel.Jmeno  

                        end }}</span>
                </td>
                <td>
                    {{ item.NazevZakazky }}
                </td>
                <td>
                    <b>
                        {{ if item.KonecnaHodnotaBezDPH != null }}
                        
                            {{ fn_FormatPrice item.KonecnaHodnotaBezDPH }}
                        
                        {{ else if ((fn_IsNullOrEmpty item.OdhadovanaHodnotaBezDPH) == false) }}
                        
                            <span>odhad.cena </span> {{ fn_FormatPrice item.OdhadovanaHodnotaBezDPH }}
                        
                        {{ else }}
                        
                                <span></span>
                        {{ end }}
                    </b>
                </td>
            </tr>
            <tr style='border-bottom:2px #777 solid'>
                <td colspan='7'>
                    <span style='font-size:80%'>
                        {{ fn_ShortenText item.PopisZakazky 300 }}
                    </span>
                </td>
            </tr>
        {{ end }}

        {{ if (model.Total > 5) }}

            <tr><td colspan='7' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
            <tr><td colspan='7' style='font-size:80%;border-bottom:1px #ddd solid'>    

                <a href='https://www.hlidacstatu.cz/verejnezakazky/hledat?Q={{ html.url_encode model.SearchQuery }}&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
                    {{ fn_Pluralize (model.Total - 5) '' 'Další nalezená zakázka' 'Další {0} nalezené zakázky' 'Dalších {0} nalezených zakázek' }} 
                </a>.
            </td></tr>
            <tr><td colspan='7' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
        {{ end }}
      
    </tbody>
</table>
";

        static string TextTemplate = @"
{{ for item in model.Items limit:#LIMIT# }}
------------------------------------------------------
Zadavatel: {{ if (fn_IsNullOrEmpty item.Zadavatel )}} neuveden{{else}}{{item.Zadavatel.Jmeno}}{{end}} 
Datum uveřejnění: {{ if (fn_IsNullOrEmpty item.DatumUverejneni)}} {{else}} {{fn_FormatDate item.DatumUverejneni 'dd.MM.yyyy'}} {{end}}
Zakázka: {{ item.NazevZakazky }}
{{ if ( fn_IsNullOrEmpty item.KonecnaHodnotaBezDPH) == false }}
Cena: {{fn_FormatPrice item.KonecnaHodnotaBezDPH html: false}}
{{ else if (fn_IsNullOrEmpty item.OdhadovanaHodnotaBezDPH) == false }}
Odhadovaná cena: {{ fn_FormatPrice item.OdhadovanaHodnotaBezDPH  html: false}}
{{end }}
Více: https://www.hlidacstatu.cz/VerejneZakazky/Zakazka/{{ item.Id }}?utm_source=hlidac&utm_medium=emailtxt&utm_campaign=detail
======================================================
{{ end }}

{{ if (model.Total > #LIMIT#) }}
{{ fn_Pluralize (model.Total - #LIMIT#) '' 'Další nalezená zakázka' 'Další {0} nalezené zakázky' 'Dalších {0} nalezených zakázek' }} na https://www.hlidacstatu.cz/verejnezakazky/hledat?Q=@(Raw(System.Web.HttpUtility.UrlEncode(model.SpecificQuery)))&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
{{ end }}

";
    }
}