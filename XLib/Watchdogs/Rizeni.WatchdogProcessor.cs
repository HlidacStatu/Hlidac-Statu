﻿using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Watchdogs
{
    public class RizeniWatchdogProcessor : IWatchdogProcessor
    {
        bool isLimited = true;

        public WatchDog OrigWD { get; private set; }

        public RizeniWatchdogProcessor(WatchDog wd)
        {
            OrigWD = wd;

            isLimited = !(
                wd.User().IsInRole("novinar")
                ||
                wd.User().IsInRole("Admin")
            );
        }

        public async Task<DateTime> GetLatestRecAsync(DateTime toDate)
        {
            var query = "posledniZmena:" +
                        string.Format("[* TO {0}]", Repositories.Searching.Tools.ToElasticDate(toDate));
            var res = await InsolvenceRepo.Searching.SimpleFulltextSearchAsync(query, 0, 1,
                (int)Repositories.Searching.InsolvenceFulltextSearchResult.InsolvenceOrderResult.LatestUpdateDesc,
                false, isLimited);

            if (res.IsValid == false)
                return DateTime.Now.Date.AddYears(-10);
            if (res.Total == 0)
                return DateTime.Now;
            return res.ElasticResults.Hits.First().Source
                .Rizeni.PosledniZmena; // ?? res.ElasticResults.Hits.First().Source.LastUpdated.Value;
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

            var res = await InsolvenceRepo.Searching.SimpleSearchAsync(query, 0, 50,
                order == null
                    ? (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc
                    : Convert.ToInt32(order),
                false, isLimited);

            return new Results(res.ElasticResults.Hits.Select(m => (dynamic)m.Source), res.Total,
                query, fromDate, toDate, res.IsValid, nameof(Rizeni));
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
            ret.ContentTitle = "Insolvenční řízení";


            return Task.FromResult(ret);
        }

        public static string HtmlTemplate = @"
{{func subjHtml 
   t =  $0 
   dn = ''
   if (t.DatumNarozeni != null)
     dn = fn_FormatDate t.DatumNarozeni 'yyyy'
   end

   if ((fn_IsNullOrEmpty t.ICO )==false)
   }}
     <a href='/subjekt/{{t.ICO}}'>{{ fn_ShortenText t.PlneJmeno  30 }}</a> IČ:{{ t.ICO }}
   {{ else if (fn_IsNullOrEmpty t.OsobaId) == false }}
      <a href='/osoba/{{t.OsobaId}}'>{{fn_ShortenText t.PlneJmeno  30 }}</a>{{dn}}
   {{ else }}
       {{ t.PlneJmeno}} {{ dn }}
  {{ end }}

{{end
}}

    <table border='0' cellpadding='4' width='100%'>
        <thead>
            <tr>
                <th></th>
                <th>Dlužník</th>
                <th>Věritelé</th>
                <th>Poslední změna</th>
                <th>Datum vzniku</th>
                <th>Stav řízení</th>
            </tr>
        </thead>
        <tbody>
        {{ for item in model.Items limit:#LIMIT# }}
            <tr>
                <td><a href='https://www.hlidacstatu.cz/Insolvence/Rizeni/{{ string.replace (string.replace item.SpisovaZnacka ' '  '_') '/'  '-' }}?qs={{html.url_encode model.SearchQuery}}&utm_source=hlidac&utm_medium=email&utm_campaign=detail'>Řízení</a></td>
                <td>
                    {{ if (item.Dluznici  && item.Dluznici.size > 0) }}
                    
                        {{ subjHtml item.Dluznici[0] }}
                        {{if (item.Dluznici.size > 1) }}
                        
                            <div>{{ fn_Pluralize (item.Dluznici.size - 1) '' 'a jeden další dlužník'  'a další {0} dlužníci'  'a dalších {0} dlužníků' }}</div>
                        {{end}}
                    {{end }}
                </td>
                <td>
                    {{ if (item.Veritele  && item.Veritele.size > 0) }}

                        {{ subjHtml item.Veritele[0] }}
                        {{ if (item.Veritele.size > 1) }}
                        
                            <div>{{ fn_Pluralize (item.Veritele.size - 1) '' 'a jeden další věřitel'  'a další {0} věřitelé'  'a dalších {0} věřitelů' }}</div>
                        {{end }}
                    {{end}}
                </td>
                <td>
                    {{ if (item.PosledniZmena )
                         fn_FormatDate item.PosledniZmena 'd.M.yyyy'
                         else 
                         'neuvedena'
                         end }}
                </td>
                <td>
                    {{ if (item.DatumZalozeni )
                        fn_FormatDate item.DatumZalozeni 'd.M.yyyy'
                     else 
                        'neuvedena'

                     end }}
                </td>
                <td>
                    {{ item.Stav }}
                </td>
            </tr>
        {{ end }}

        {{ if (model.Total > 5) }}

            <tr><td colspan='6' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
            <tr><td colspan='6'>    
                <a href='https://www.hlidacstatu.cz/insolvence/hledatFtx?Q={{ html.url_encode model.SearchQuery }}&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
                    {{ fn_Pluralize (model.Total - 5) '' 'Další nalezená insolvence' 'Další {0} nalezené insolvence' 'Dalších {0} nalezených insolvencí' }} 
                </a>.
            </td></tr>
            <tr><td colspan='6' height='30' style='line-height: 30px; min-height: 30px;'></td></tr>
        {{ end }}
    </table>
";

        static string TextTemplate = @"
{{ for item in model.Items limit:#LIMIT# }}
------------------------------------------------------
Dlužníci:{{ if (item.Dluznici  && item.Dluznici.size > 0)}}{{item.Dluznici[0].PlneJmeno}}{{else}} neuveden{{end }}
Věřitelé:{{ if (item.Veritele && item.Veritele.size > 0)}}{{item.Veritele[0].PlneJmeno}}{{else}}neuveden{{end }}
Poslední změna: {{ fn_FormatDate item.PosledniZmena 'd.M.yyyy' }}
Datum vzniku insolvence: {{ fn_FormatDate item.DatumZalozeni 'd.M.yyyy' }}
Stav řízení: {{ item.Stav }}
Více: https://www.hlidacstatu.cz/Insolvence/Rizeni/{{ string.replace (string.replace item.SpisovaZnacka ' '  '_') '/'  '-' }}?utm_source=hlidac&utm_medium=emailtxt&utm_campaign=detail
{{end }}
======================================================

{{ if (model.Total > #LIMIT#) }}
{{ fn_Pluralize (model.Total - #LIMIT#) '' 'Další nalezená insolvence' 'Další {0} nalezené insolvence' 'Dalších {0} nalezených insolvencí' }} na https://www.hlidacstatu.cz/verejnezakazky/hledat?Q=@(Raw(System.Web.HttpUtility.UrlEncode(model.SpecificQuery)))&utm_source=hlidac&utm_medium=emailtxt&utm_campaign=more'>
{{ end }}

";
    }
}