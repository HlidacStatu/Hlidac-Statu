using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.RPP
{
    public partial class RPP
    {
        HttpClient wc = null;
        Devmasters.Net.HttpClient.UrlContentContext authCook = null;
        string root = "https://rpp-ais.egon.gov.cz";
        public RPP()
        {
            using (var net = new Devmasters.Net.HttpClient.URLContent(root + "/AISP/verejne/ovm-spuu/katalog-kategorii-ovm"))
            {
                var res = net.GetContent();
                authCook = res.Context;
            }
            var wch = new HttpClientHandler() { CookieContainer = new System.Net.CookieContainer() };
            wch.CookieContainer.Add(authCook.Cookies.First(m=>m.Name.StartsWith("BIGipServerais")));
            wc = new HttpClient(wch);
            wc.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            wc.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            wc.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            wc.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            wc.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
            //wc.DefaultRequestHeaders.Add("Cookie", authCook.Cookies["JSessionID"].ToString());

        }

        /*
         PUT https://rpp-ais.egon.gov.cz/AISP/rest/verejne/katovm?start=0&pocet=10&razeni=-datumPosledniZmeny,-id HTTP/1.1
Host: rpp-ais.egon.gov.cz
Connection: keep-alive
Content-Length: 17
Accept: application/json
DNT: 1
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36
Content-Type: application/json
Origin: https://rpp-ais.egon.gov.cz
Sec-Fetch-Site: same-origin
Sec-Fetch-Mode: cors
Sec-Fetch-Dest: empty
Referer: https://rpp-ais.egon.gov.cz/AISP/verejne/ovm-spuu/katalog-kategorii-ovm
Accept-Encoding: gzip, deflate, br
Accept-Language: cs,en-US;q=0.9,en;q=0.8,sk;q=0.7
Cookie: _ga=GA1.2.1208733850.1580933391; JSessionID=R3890696782

         
         */
        public async Task<ResultList<KategorieOVM>> KategorieOVMAsync()
        {

            string payload = "{\"stav\":[\"SCHVALENO\",\"UKONCENO\"],\"primarni\":true}";

            var json = await CallAsync(root + "/AISP/rest/verejne/katovm?start=0&pocet=1000&razeni=-datumPosledniZmeny,-id",
                payload,
                "PUT"
                );

            var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<KategorieOVM>>(json);

            var prefSeznam = res.seznam.ToArray();
            for (int i = 0; i < prefSeznam.Count(); i++)
            {
                prefSeznam[i].hlidac_preferred = External.RPP.KategorieOVM.PreferredIds.Contains(prefSeznam[i].id);
            }
            res.seznam = prefSeznam.ToArray();
            return res;
        }

        public async Task<ResultList<OVMSimple>> OVM_v_KategoriiOVMAsync(KategorieOVM katOVM)
        {
            List<OVMSimple> ovms = new List<OVMSimple>();

            int step = 1000;
            int from = 0;
            do
            {
                var json = await CallAsync(root + $"/AISP/rest/verejne/ovmkat/{katOVM.id}/ovmclenystrankovane?start={from}&pocet={step}&razeni=-id,-id",
                    "{}",
                    "PUT"
                    );

                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<OVMSimple>>(json);
                if (res == null)
                    break;
                if (res.seznam.Count() == 0)
                    break;
                ovms.AddRange(res.seznam);

                if (res.pocetCelkem < step)
                    break;

                from = from + step;
            } while (true);

            return new ResultList<OVMSimple>()
            {
                pocetCelkem = ovms.Count,
                seznam = ovms
            };
        }

        public async Task<ResultList<ISVS>> ISVSListAsync()
        {
            List<ISVS> ovms = new List<ISVS>();

            int step = 100;
            int from = 0;
            do
            {
                var json = await CallAsync(root + $"/AISP/rest/verejne/isvs?start={from}&pocet={step}&razeni=-datumPosledniZmeny,-id",
                    "{\"stav\":[\"SCHVALENO\",\"UKONCENO\"],\"omezitPodlePrihlaseneho\":false,\"primarni\":false}",
                    "PUT"
                    );

                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS>>(json);
                if (res.seznam.Count() == 0)
                    break;
                ovms.AddRange(res.seznam);

                if (res.pocetCelkem < step)
                    break;

                from = from + step;
            } while (true);

            return new ResultList<ISVS>()
            {
                pocetCelkem = ovms.Count,
                seznam = ovms
            };
        }

        public async Task<ISVS> ISVSDetailAsync(int isvsId)
        {
            var json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}",
                "",
                "GET"
                );
            var isvs = Newtonsoft.Json.JsonConvert.DeserializeObject<ISVS>(json);

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/vypisvyuziti");
            isvs.vyuzitiIS = Newtonsoft.Json.JsonConvert.DeserializeObject<ISVS.vypisvyuziti>(json);

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/polozkystruktury");
            isvs.aplikacniCleneni = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS.aplikacnicleneni>>(json)?.seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/pp");
            isvs.pravniPredpisy = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS.pravnipredpisy>>(json)?.seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/milniky");
            isvs.milnikyZivotnihoCyklu = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS.milniky>>(json)?.seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/gdpr");
            isvs.Gdpr = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS.gdpr>>(json)?.seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/finance");
            isvs.financniUdaje = Newtonsoft.Json.JsonConvert.DeserializeObject<ISVS.finance>(json);

            json = await CallAsync(root + $"/AISP/rest/verejne/isvs/{isvsId}/subjekty");
            var subj = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<ISVS.Subjekt>>(json)?.seznam?.ToArray();
            if (subj != null)
            {
                isvs.Dodavatele = subj.Where(m => m.typSubjektu.ToUpper() == "DODAVATEL").ToArray();
                isvs.Spravci = subj.Where(m => m.typSubjektu.ToUpper() == "SPRAVCE").ToArray();
                isvs.Provozovatele = subj.Where(m => m.typSubjektu.ToUpper() == "PROVOZOVATEL").ToArray();
            }


            return isvs;
        }


        public async Task<OVMFull> OVMDetailAsync(OVMSimple ovm)
        {
            return await OVMDetailAsync(ovm.id);
        }
        public async Task<OVMFull> OVMDetailAsync(int ovmId)
        {
            var json = await CallAsync(root + $"/AISP/rest/verejne/ovm/{ovmId}/hlavniatributy",
                "",
                "GET"
                );
            var ovm = Newtonsoft.Json.JsonConvert.DeserializeObject<OVMFull>(json);

            json = await CallAsync(root + $"/AISP/rest/verejne/ovm/{ovmId}/podrizenaovm",
                "",
                "GET"
                );


            ovm.podrizeneOVM = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<OVMSimple.Ovm>>(json).seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/ovm/{ovmId}/osobyvcele",
                "",
                "GET"
                );

            ovm.angazovaneOsoby = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<OVMFull.Osoba>>(json).seznam?.ToArray();

            json = await CallAsync(root + $"/AISP/rest/verejne/ovm/{ovmId}/osobaros",
                "",
                "GET"
                );

            var osobaros = Newtonsoft.Json.JsonConvert.DeserializeObject<osobaros>(json);
            ovm.kodPravnihoStavu = osobaros?.kodPravnihoStavu ?? 0;
            ovm.nazevPravnihoStavu = osobaros?.nazevPravnihoStavu;
            ovm.verejnaProspesnost = osobaros?.verejnaProspesnost ?? false;
            ovm.angazovaneOsoby = osobaros?.angazovaneOsoby;

            json = await CallAsync(root + $"/AISP/rest/verejne/ovm/{ovmId}/seznamkatprirazenychkovm?start=0&pocet=1000&razeni=-id,-id",
                "",
                "GET"
                );
            ovm.kategorie = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultList<seznamkatprirazenychkovm>>(json)
                .seznam?
                .Select(m => m.kategorieOvm)
                .ToArray();


            return ovm;
        }
        public async Task<string> CallAsync(string url, string body = null, string method = "GET")
        {
            method = method.ToUpper();


            try
            {
                HttpResponseMessage response = null;
                StringContent content = null;
                if (!string.IsNullOrEmpty(body))
                    content = new StringContent(body, Encoding.UTF8, "application/json");

                if (method == "GET")
                    response = await wc.GetAsync(url);
                else if (method == "PUT")
                    response = await wc.PutAsync(url, content);
                else if (method == "POST")
                    response = await wc.PostAsync(url, content);
                else if (method == "DELETE")
                    response = await wc.DeleteAsync(url);
                response?.EnsureSuccessStatusCode();
                string responseBody = await response?.Content?.ReadAsStringAsync();
                
                return responseBody;
            }
            catch (HttpRequestException)
            {
                return string.Empty;
            }
        }

    }
}

