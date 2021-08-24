using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using HlidacStatu.Util;
using Polly;
using Polly.Retry;

namespace HlidacStatu.Plugin.TransparetniUcty
{
    public class KB : BaseTransparentniUcetParser
    {
        public override string Name => "KB";

        // which status codes should trigger retry
        private static readonly HttpStatusCode[] HttpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        // definition of retry policy
        private readonly RetryPolicy<HttpResponseMessage> RetryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => HttpStatusCodesWorthRetrying.Contains(r.StatusCode))
            .WaitAndRetry(new[]
            {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(30)
            });

        public KB(IBankovniUcet ucet) : base(MapOldUrls(ucet))
        { }

        protected override IEnumerable<IBankovniPolozka> DoParse(DateTime? fromDate = null, DateTime? toDate = null)
        {
            string apiUrl = "https://www.kb.cz/transparentsapi/transactions/"; //api base address
            int chunk = 10000; // how many items should be loaded
            
            var polozky = new List<IBankovniPolozka>();
            
            TULogger.Info($"Zpracovavam ucet {Ucet.CisloUctu} s url {Ucet.Url}");
            string cisloUctuBezKoncovky = Ucet.CisloUctu.Split('/', 
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(cisloUctuBezKoncovky))
            {
                TULogger.Info($"Nedokazu oddelit cislo uctu od kodu banky. Cislo uctu [{Ucet.CisloUctu}]");
                return polozky;
            }
            
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(apiUrl);
            httpClient.Timeout = TimeSpan.FromMinutes(3);

            var page = 0;
            bool containsNextPage = false;

            do
            {
                var response = RetryPolicy.Execute(() =>
                {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get, $"{cisloUctuBezKoncovky}?skip={page * chunk}&size={chunk}");
                    return httpClient.Send(request);
                });

            
                if (response.IsSuccessStatusCode)
                {
                    using var reader = new StreamReader(response.Content.ReadAsStream());
                    var content = reader.ReadToEnd();
                    var result = JsonSerializer.Deserialize<Root>(content);

                    if (result?.items is null || result?.items?.Count() == 0)
                    {
                        TULogger.Info($"Ucet neobsahuje zadne polozky.");
                        break;
                    }
                        
                    foreach (var item in result.items)
                    {
                        DateTime datum = ParseDate(item.date);
                        if (fromDate != null && datum < fromDate)
                            continue;
                        if (toDate != null && datum > toDate)
                            continue;
                        
                        var itemSymbols = item.symbols.Replace(" ", "").Split('/');
                        var notes = item.notes.Split("<br />");
                        string nazevProtiuctu = "";
                        string popisTransakce = "";
                        string zpravaPrijemci = "";
                        if (notes.Length == 3) // příchozí transakce
                        {
                            popisTransakce = notes[1];
                            nazevProtiuctu = notes[0];
                            zpravaPrijemci = notes[2];
                        }
                        if (notes.Length == 2) // odchozí transakce
                        {
                            popisTransakce = notes[0];
                            nazevProtiuctu = notes[1];
                        }
                        
                        polozky.Add(new SimpleBankovniPolozka
                        {
                            AddId = item.id,
                            CisloUctu = Ucet.CisloUctu,
                            Castka = ParsePrice(item.amount, datum),
                            Datum = datum,
                            VS = itemSymbols.Length >= 1 ? itemSymbols[0] : "",
                            KS = itemSymbols.Length >= 2 ? itemSymbols[1] : "",
                            SS = itemSymbols.Length >= 3 ? itemSymbols[2] : "",
                            NazevProtiuctu = nazevProtiuctu,
                            PopisTransakce = popisTransakce,
                            ZpravaProPrijemce = zpravaPrijemci,
                            ZdrojUrl = apiUrl + cisloUctuBezKoncovky,
                            CisloProtiuctu = ""
                        });
                    }
                    
                    containsNextPage = result?.loadMore ?? false;
                }
                else
                {
                    TULogger.Info($"Chyba {response.StatusCode}");
                    break;
                }
                
                page++;
            } while (containsNextPage);


            
            return polozky;

        }
        
        
        public class Item
        {
            public string id { get; set; }
            public string date { get; set; }
            public string amount { get; set; }
            public string symbols { get; set; }
            public string notes { get; set; }
        }

        public class Root
        {
            public bool loadMore { get; set; }
            public List<Item> items { get; set; }

        }

        

        private bool IsAlreadyExist(List<IBankovniPolozka> polozky, IBankovniPolozka p)
        {
            return polozky.Exists(i => i.Castka == p.Castka && i.Datum == p.Datum &&
                                       i.CisloProtiuctu == p.CisloProtiuctu
                                       && i.VS == p.VS && i.KS == p.KS && i.SS == p.SS &&
                                       i.ZpravaProPrijemce == p.ZpravaProPrijemce);
        }

        private decimal ParsePrice(string value, DateTime date)
        {
            var price = ParseTools.ToDecimal(WebUtility.HtmlDecode(value).Replace(" CZK", "").Replace(" ", ""));
            if (price.HasValue)
            {
                return price.Value;
            }

            TULogger.Error($"KB: chybejici castka pro ucet {Ucet.CisloUctu} dne {date}");
            throw new ApplicationException($"KB: chybejici castka pro ucet {Ucet.CisloUctu} dne {date}");
        }

        private DateTime ParseDate(string value)
        {
            
            var dat = Devmasters.DT.Util.ToDateTime(value.Replace("&nbsp;", " "), "d. M. yyyy");
            if (dat.HasValue)
            {
                return dat.Value;
            }

            TULogger.Error($"KB: chybejici datum pro ucet {Ucet.CisloUctu}");
            throw new ApplicationException($"KB: chybejici datum pro ucet {Ucet.CisloUctu}");
        }

        
        private string MakeRequest(int page, HttpClient httpClient)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(
                    "p_lt_WebPartZone6_zoneContent_contentPH_p_lt_WebPartZone3_zoneContentTop_TransactionsActum_plcUp_repItems_pager_cpage",
                    page.ToString())
            });
            content.Headers.Add("X-Requested-With", "XMLHttpRequest");
            content.Headers.Add("X-MicrosoftAjax", "Delta=true");
            var s = httpClient.PostAsync(Ucet.Url, content).Result.Content.ReadAsStringAsync().Result;
            return s;
        }

        private static IBankovniUcet MapOldUrls(IBankovniUcet ucet)
        {
            if (OldUrlMapping.ContainsKey(ucet.CisloUctu))
            {
                ucet.Url = OldUrlMapping[ucet.CisloUctu];
            }
            return ucet;
        }

        private static Dictionary<string, string> OldUrlMapping = new Dictionary<string, string>
        {
            { "115-5587300207/0100","https://www.kb.cz/cs/transparentni-ucty/hozk-hozk-czk" },
            { "115-5750690277/0100","https://www.kb.cz/cs/transparentni-ucty/karlovaraci-karlovaraci-czk" },
            { "115-5223490257/0100","https://www.kb.cz/cs/transparentni-ucty/sdruzeni-nezavislych-obcanu-prahy-sdruzeni-nezavi" },
            { "115-4449010207/0100","https://www.kb.cz/cs/transparentni-ucty/snk-evropsti-demokrate-snk-evropsti-demokrate-czk" },
            { "115-5547140237/0100","https://www.kb.cz/cs/transparentni-ucty/b-u-prav-os-s-volyne-v-dolyne-czk" },
            { "115-2573210207/0100","https://www.kb.cz/cs/transparentni-ucty/dobra-volba-2016-dobra-volba-2016-czk" },
            { "115-5471580227/0100","https://www.kb.cz/cs/transparentni-ucty/bilinsti-socialni-demokrate-bilinsti-socialni-dem" },
            { "115-5087400287/0100","https://www.kb.cz/cs/transparentni-ucty/radostne-cesko-transparentni-2-radostne-cesko-c" },
            { "115-4035960237/0100","https://www.kb.cz/cs/transparentni-ucty/plus-plus-czk" },
            { "35-7608650257/0100","https://www.kb.cz/cs/transparentni-ucty/nestranici-nestranici-czk-(1)" },
            { "115-5551500207/0100","https://www.kb.cz/cs/transparentni-ucty/moderni-spolecnost-moderni-spolecnost-czk" },
            { "115-2638450237/0100","https://www.kb.cz/cs/transparentni-ucty/prokraj-prokraj-czk" },
            { "107-8866160247/0100","https://www.kb.cz/cs/transparentni-ucty/profi-ucet-prozmenu-czk" },
            { "107-7430660297/0100","https://www.kb.cz/cs/transparentni-ucty/ods-transparentni-ucet-obcanska-demokraticka-stra" },
            { "115-3887350207/0100","https://www.kb.cz/cs/transparentni-ucty/severocesi-cz-severocesi-cz-czk" },
            { "115-5510480297/0100","https://www.kb.cz/cs/transparentni-ucty/ceska-pravice-michal-simkanic-czk" },
            { "115-6659080237/0100","https://www.kb.cz/cs/transparentni-ucty/ostravak-mudr-tomas-malek-czk" },
            { "115-5527330287/0100","https://www.kb.cz/cs/transparentni-ucty/kozy-ucet-pro-prispevky-adela-sochurkova-czk" },
            { "51-7126270227/0100","https://www.kb.cz/cs/transparentni-ucty/volba-pro-mladou-boleslav-volba-pro-mladou-bolesl" },
            { "115-3902720297/0100","https://www.kb.cz/cs/transparentni-ucty/strana-soukromniku-ceske-republiky-strana-soukrom" },
            { "4070217/0100","https://www.kb.cz/cs/transparentni-ucty/ano-2011-ano-2011-czk" },
            { "115-5010530227/0100","https://www.kb.cz/cs/transparentni-ucty/novy-impuls-novy-impuls-czk" },
            { "115-5000730217/0100","https://www.kb.cz/cs/transparentni-ucty/hnuti-praha-kunratice-hnuti-praha-kunratice" },
            { "115-5087380247/0100","https://www.kb.cz/cs/transparentni-ucty/radostne-cesko-transparentni-1-radostne-cesko-c" },
            { "115-4933560227/0100","https://www.kb.cz/cs/transparentni-ucty/jirkov-21-stoleti-jirkov-21-stoleti-czk" },
            { "107-7678680267/0100","https://www.kb.cz/cs/transparentni-ucty/toryove-toryove-czk" },
            { "115-5053730237/0100","https://www.kb.cz/cs/transparentni-ucty/dobra-volba-2016-transp-volebni-ucet-dobra-vol" },
            { "115-5646610257/0100","https://www.kb.cz/cs/transparentni-ucty/obcane-spolu-nezavisli-obcane-spolu-nezavisli" },
            { "4080247/0100","https://www.kb.cz/cs/transparentni-ucty/ano-2011-volebni-ucet-ano-2011-czk" },
        };
    }
}
