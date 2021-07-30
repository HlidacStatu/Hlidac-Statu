using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using HlidacStatu.Api.V2.CoreApi.Client;
using HlidacStatu.Api.V2.Dataset;

using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;


/*
https://archi.gov.cz/nap_dokument:architektura_a_sdilene_sluzby_verejne_spravy_cr#agendovy_model_verejne_spravy

Logika:
Agenda -> Cinnost (výčet a popis činností, které mají být vykonávány v agendě) 
       -> Ukony (výčet a popis úkonů OVM )


OVM 00215724  -> poskytuje sluzby -> sluzba služba/S15719 -> pracoviste
                                    - sluzba spada do agendy agenda/A120, úkony 

          -> poskytuje cinnost činnost/A120/CR835

 */

namespace AgendyOVM
{
    class Program
    {
        private const string DatasetNameAgendy = "rpp-agendy-ovm";
        private const string DatasetNameUkony = "rpp-ukony";
        private const string DatasetNamePusobnost = "rpp-subjekt-agendy";
        private const string DatasetNamePracoviste = "rpp-pracoviste";

        public static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("AgendyOVM");

        static List<OVM.PracovištěOvm> pracoviste = new List<OVM.PracovištěOvm>();
        static Ukony.Ukon[] ukony = null;

        //static HlidacStatu.Api.V2.Dataset.Typed.Dataset<majitel_flat> ds_flat = null;
        public static string apiKey = "";
        public static bool force = false;

        static Devmasters.Cache.File.FileCache<Sluzby.Sluzba[]> sluzby = new Devmasters.Cache.File.FileCache<Sluzby.Sluzba[]>("", TimeSpan.FromDays(2), "sluzby",
                obj =>
                {
                    var wc = new System.Net.WebClient();
                    var json = wc.DownloadString("https://rpp-opendata.egon.gov.cz/odrpp/datovasada/sluzby.json");
                    var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Sluzby.All>(json);
                    return all.Položky;
                });
        static Devmasters.Cache.File.FileCache<OVM.Položky[]> ovm = new Devmasters.Cache.File.FileCache<OVM.Položky[]>("", TimeSpan.FromDays(2), "ovm",
                obj =>
                {
                    var wc = new System.Net.WebClient();
                    var json = wc.DownloadString("https://rpp-opendata.egon.gov.cz/odrpp/datovasada/ovm.json");
                    var all = Newtonsoft.Json.JsonConvert.DeserializeObject<OVM.Root>(json);
                    return all.Položky;
                });

        static void Main(string[] parameters)
        {
            Console.WriteLine("AgendyOVM");
            var args = new Devmasters.Args(parameters);
            logger.Info($"Starting with args {string.Join(' ', parameters)}");

            apiKey = args["/apikey"];
            force = args.Exists("/force");


            HlidacStatu.Api.V2.CoreApi.Model.Registration reg = new HlidacStatu.Api.V2.CoreApi.Model.Registration(
               "Seznam úkonů poskytovaných služeb státní správou", DatasetNameUkony,
               "https://data.gov.cz/datová-sada?iri=https%3A%2F%2Fdata.gov.cz%2Fzdroj%2Fdatové-sady%2F00007064%2F706529437%2F2da57720c4c2d44f128834a4f7ebb41a",
               "https://github.com/HlidacStatu/Datasety/tree/master/AgendyOVM",
               "Úkony služeb a agend evidované v Registru práv a povinností ve smyslu § 51 zákona č. 111/2009 Sb. o základních registrech.",
               null, betaversion: true, allowWriteAccess: false,
               orderList: new string[,] { },
               defaultOrderBy: "Id ",
               searchResultTemplate: new ClassicTemplate.ClassicSearchResultTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Název", "{{item.název-ukonu}}")
                ,
                detailTemplate: new ClassicTemplate.ClassicDetailTemplate()
                    .AddColumn("Název", "{{item.název-ukonu}}")
                );
            ukony = sluzby.Get()
               .Where(m => m.Úkony != null)
               .SelectMany(m => m.Úkony)
               .GroupBy(m => m.Id)
               .Select(m => m.First())
               .ToArray();
            //Create<Ukony.Ukon>(args, ukony, reg);



            reg = new HlidacStatu.Api.V2.CoreApi.Model.Registration(
               "Seznam pracovišť OVM evidovaných v Registru práv a povinností", DatasetNamePracoviste,
               "https://data.gov.cz/datová-sada?iri=https%3A%2F%2Fdata.gov.cz%2Fzdroj%2Fdatové-sady%2F00007064%2F706529437%2F2da57720c4c2d44f128834a4f7ebb41a",
               "https://github.com/HlidacStatu/Datasety/tree/master/AgendyOVM",
               "Pracoviště OVM evidovaná v Registru práv a povinností ve smyslu § 51 zákona č. 111/2009 Sb. o základních registrech.",
               null, betaversion: true, allowWriteAccess: false,
               orderList: new string[,] { },
               defaultOrderBy: "Id ",
               searchResultTemplate: new ClassicTemplate.ClassicSearchResultTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Název", "{{item.název-ukonu}}")
                ,
                detailTemplate: new ClassicTemplate.ClassicDetailTemplate()
                    .AddColumn("Název", "{{item.název-ukonu}}")
                );

            Console.WriteLine("getting Pracoviste");
            var pracTmp = ovm.Get()
               .Where(m => m.PracovištěOvm != null)
               .SelectMany(m => m.PracovištěOvm)
               .GroupBy(m => m.Id)
               .Select(m => m.First());
            object lockPrac = new object();
            Devmasters.Batch.Manager.DoActionForAll<OVM.PracovištěOvm>(pracTmp,
                    p =>
                    {
                        if (false && !string.IsNullOrEmpty(p.AdresaPr?.Kód) && string.IsNullOrWhiteSpace(p.AdresaPrTxt))
                        {
                            p.AdresaPrTxt = RUIAN.GetAdresaByMistoID(p.AdresaPr.Kód);
                        }

                        lock (lockPrac)
                            pracoviste.Add(p);

                        return new Devmasters.Batch.ActionOutputData();
                    }, true);

            //Create<OVM.PracovištěOvm>(args, prac, reg);

            CreatePusobnost(args);

        }


        private static void Create<T>(Devmasters.Args args, IEnumerable<T> data, HlidacStatu.Api.V2.CoreApi.Model.Registration reg)
            where T : class
        {
            var jsonGen = new JSchemaGenerator
            {
                DefaultRequired = Required.Default
            };

            HlidacStatu.Api.V2.Dataset.Typed.Dataset<T> ds = null;

            var genJsonSchema = jsonGen.Generate(typeof(T)).ToString();
            reg.JsonSchema = genJsonSchema;

            try
            {
                if (args.Exists("/new"))
                {
                    Configuration configuration = new Configuration();
                    configuration.AddDefaultHeader("Authorization", apiKey);
                    HlidacStatu.Api.V2.CoreApi.DatasetyApi datasetyApi = new HlidacStatu.Api.V2.CoreApi.DatasetyApi(configuration);
                    datasetyApi.ApiV2DatasetyDelete(reg.DatasetId);
                }
                ds = HlidacStatu.Api.V2.Dataset.Typed.Dataset<T>.OpenDataset(apiKey, reg.DatasetId);

            }
            catch (HlidacStatu.Api.V2.CoreApi.Client.ApiException e)
            {
                ds = HlidacStatu.Api.V2.Dataset.Typed.Dataset<T>.CreateDataset(apiKey, reg);

            }
            catch (Exception e)
            {
                throw;
            }

            Devmasters.Batch.Manager.DoActionForAll<T>(data,
          u =>
          {

              ds.AddOrUpdateItem(u, HlidacStatu.Api.V2.Dataset.Typed.ItemInsertMode.rewrite); //.AddOrRewriteItem(ag);

              return new Devmasters.Batch.ActionOutputData();
          }, Devmasters.Batch.Manager.DefaultOutputWriter, Devmasters.Batch.Manager.DefaultProgressWriter,
          !System.Diagnostics.Debugger.IsAttached,
          maxDegreeOfParallelism: 2, prefix: $"Save {typeof(T).Name} ");


        }


        private static void CreateAgendy(Devmasters.Args args)
        {
            string agUrl = "https://rpp-opendata.egon.gov.cz/odrpp/datovasada/agendy.json";
            HlidacStatu.Api.V2.Dataset.Typed.Dataset<Agendy.Agenda> dsAg = null;

            var jsonGen = new JSchemaGenerator
            {
                DefaultRequired = Required.Default
            };
            var genJsonSchema = jsonGen.Generate(typeof(Agendy.Agenda)).ToString();

            HlidacStatu.Api.V2.CoreApi.Model.Registration regAg = new HlidacStatu.Api.V2.CoreApi.Model.Registration(
   "Seznam agend OVM", DatasetNameAgendy,
   "https://data.gov.cz/datová-sada?iri=https%3A%2F%2Fdata.gov.cz%2Fzdroj%2Fdatové-sady%2F00007064%2F706529437%2F9c73b802263c5e0ccf5542f10fbc35bb",
   "https://github.com/HlidacStatu/Datasety/tree/master/AgendyOVM",
   "Agendy evidované v Registru práv a povinností ve smyslu § 51 zákona č. 111/2009 Sb. o základních registrech.",
   genJsonSchema, betaversion: true, allowWriteAccess: false,
   orderList: new string[,] {
                    { "Podle datumu zápisu", "datum_zapis" },
                    { "Podle IČ subjektu", "ico" },
   },
   defaultOrderBy: "datum_zapis desc",
   searchResultTemplate: new ClassicTemplate.ClassicSearchResultTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Kod", "{{item.Kod}}")
                    .AddColumn("Název", "{{item.Nazev}}")
                    .AddColumn("Ohlašovatel", "{{fn_RenderCompanyWithLink item.OhlasovatelICO}}")
                ,
                detailTemplate: new ClassicTemplate.ClassicDetailTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Kod", "{{item.Kod}}")
                    .AddColumn("Název", "{{item.Nazev}}")
                    .AddColumn("Ohlašovatel", "{{fn_RenderCompanyWithLink item.OhlasovatelICO}}")
                );

            try
            {
                if (args.Exists("/new"))
                {
                    Configuration configuration = new Configuration();
                    configuration.AddDefaultHeader("Authorization", apiKey);
                    HlidacStatu.Api.V2.CoreApi.DatasetyApi datasetyApi = new HlidacStatu.Api.V2.CoreApi.DatasetyApi(configuration);
                    datasetyApi.ApiV2DatasetyDelete(regAg.DatasetId);
                }
                dsAg = HlidacStatu.Api.V2.Dataset.Typed.Dataset<Agendy.Agenda>.OpenDataset(apiKey, regAg.DatasetId);

            }
            catch (HlidacStatu.Api.V2.CoreApi.Client.ApiException e)
            {
                dsAg = HlidacStatu.Api.V2.Dataset.Typed.Dataset<Agendy.Agenda>.CreateDataset(apiKey, regAg);

            }
            catch (Exception e)
            {
                throw;
            }

            var wc = new System.Net.WebClient();
            var json = wc.DownloadString(agUrl);

            var agendyRaw = Newtonsoft.Json.JsonConvert.DeserializeObject<Agendy.Raw>(json);
            //var agendyRaw2 = Newtonsoft.Json.Linq.JObject.Parse(json);

            Devmasters.Batch.Manager.DoActionForAll<Agendy.Raw.PoložkyStruct>(agendyRaw.Položky,
          agR =>
          {
              Agendy.Agenda ag = new Agendy.Agenda();
              ag.Id = agR.Id.Replace("agenda/", "");
              ag.Kod = agR.Kód;
              ag.HlavniUstanovení = agR.HlavníUstanovení?.Select(m =>
                  new Agendy.OznaceniType()
                  {
                      Oznaceni = m.Označení,
                      Type = m.Type
                  })?.ToArray();
              ag.Nazev = agR.Název.Cs;
              ag.OhlasovatelIco = Devmasters.RegexUtil.GetRegexGroupValue(agR.Ohlašovatel, ".*/(?<ic>\\d*)", "ic");
              ag.PlatnostDo = agR.PlatnostDo?.DateTime;
              ag.PlatnostOd = agR.PlatnostOd?.DateTime;
              ag.Type = agR.Type;
              ag.Ustanoveni = agR.Ustanovení?.Select(m => new Agendy.OznaceniType()
              {
                  Oznaceni = m.Označení,
                  Type = m.Type
              })?.ToArray();
              ag.VykonavajiciKategorieOvm = agR.VykonávajícíKategorieOvm;
              ag.VykonavajiciKategorieSpuu = agR.VykonávajícíKategorieSpuú;
              ag.VykonavajiciOvm = agR.VykonávajícíOvm;
              ag.VykonavajiciSpuu = agR.VykonávajícíSpuú;
              ag.Vznik = agR.Vznik?.DateTime;
              ag.Zanik = agR.Zánik?.DateTime;
              ag.Cinnosti = agR.Činnosti?.Select(m =>
              new Agendy.Cinnosti()
              {
                  Id = m.Id.Replace("činnost/", ""),
                  KodCinnosti = m.KódČinnosti,
                  NazevCinnosti = m.NázevČinnosti.Cs,
                  PopisCinnosti = m.PopisČinnosti.Cs,
                  TypCinnosti = m.TypČinnosti,
                  Type = m.Type,
                  PlatnostCinnostiDo = m.PlatnostČinnostiDo?.DateTime,
                  PlatnostCinnostiOd = m.PlatnostČinnostiOd?.DateTime
              }
              )?.ToArray();

              dsAg.AddOrUpdateItem(ag, HlidacStatu.Api.V2.Dataset.Typed.ItemInsertMode.rewrite); //.AddOrRewriteItem(ag);

              return new Devmasters.Batch.ActionOutputData();
          }, Devmasters.Batch.Manager.DefaultOutputWriter, Devmasters.Batch.Manager.DefaultProgressWriter,
          !System.Diagnostics.Debugger.IsAttached,
          maxDegreeOfParallelism: 2, prefix: "save agendy ");


        }



        private static void CreatePusobnost(Devmasters.Args args)
        {
            string url = "https://rpp-opendata.egon.gov.cz/odrpp/datovasada/pusobnost_v_agendach.json";
            HlidacStatu.Api.V2.Dataset.Typed.Dataset<pusobnost_v_agendach.Pusobnosti.Pusobnost> ds = null;

            var jsonGen = new JSchemaGenerator
            {
                DefaultRequired = Required.Default
            };
            var genJsonSchema = jsonGen.Generate(typeof(pusobnost_v_agendach.Pusobnosti.Pusobnost)).ToString();

            HlidacStatu.Api.V2.CoreApi.Model.Registration reg = new HlidacStatu.Api.V2.CoreApi.Model.Registration(
   "Agendy jednotlivých OVM a soukromoprávních uživatelů", DatasetNamePusobnost,
   "https://data.gov.cz/datová-sada?iri=https%3A%2F%2Fdata.gov.cz%2Fzdroj%2Fdatové-sady%2F00007064%2F706529437%2F7ee91a644bf6da16a9f5c4f337163c0f",
   "https://github.com/HlidacStatu/Datasety/tree/master/AgendyOVM",
   "Působnosti orgánů veřejné moci nebo soukromoprávních uživatelů údajů v agendách evidované v Registru práv a povinností ve smyslu § 51 zákona č. 111/2009 Sb. o základních registrech.",
   genJsonSchema, betaversion: true, allowWriteAccess: false,
   orderList: new string[,] {
                    { "Podle datumu zápisu", "datum_zapis" },
                    { "Podle IČ subjektu", "ico" },
   },
   defaultOrderBy: "datum_zapis desc",
   searchResultTemplate: new ClassicTemplate.ClassicSearchResultTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Kod", "{{item.Kod}}")
                    .AddColumn("Název", "{{item.Nazev}}")
                    .AddColumn("Ohlašovatel", "{{fn_RenderCompanyWithLink item.OhlasovatelICO}}")
                ,
                detailTemplate: new ClassicTemplate.ClassicDetailTemplate()
                    .AddColumn("Id", @"<a href=""{{ fn_DatasetItemUrl item.id }}"">{{item.id}}</a>")
                    .AddColumn("Kod", "{{item.Kod}}")
                    .AddColumn("Název", "{{item.Nazev}}")
                    .AddColumn("Ohlašovatel", "{{fn_RenderCompanyWithLink item.OhlasovatelICO}}")
                );

            try
            {
                if (args.Exists("/new"))
                {
                    Configuration configuration = new Configuration();
                    configuration.AddDefaultHeader("Authorization", apiKey);
                    HlidacStatu.Api.V2.CoreApi.DatasetyApi datasetyApi = new HlidacStatu.Api.V2.CoreApi.DatasetyApi(configuration);
                    datasetyApi.ApiV2DatasetyDelete(reg.DatasetId);
                }
                ds = HlidacStatu.Api.V2.Dataset.Typed.Dataset<pusobnost_v_agendach.Pusobnosti.Pusobnost>.OpenDataset(apiKey, reg.DatasetId);

            }
            catch (HlidacStatu.Api.V2.CoreApi.Client.ApiException e)
            {
                ds = HlidacStatu.Api.V2.Dataset.Typed.Dataset<pusobnost_v_agendach.Pusobnosti.Pusobnost>.CreateDataset(apiKey, reg);

            }
            catch (Exception e)
            {
                throw;
            }

            Console.WriteLine("Downloading pusobnost");
            //var wc = new System.Net.WebClient();
            //wc.DownloadFile(url,"pusobnost_v_agendach.json");
            Console.WriteLine("deserializing pusobnost");

            pusobnost_v_agendach.Pusobnosti.Raw raw = null;
            using (var stream = System.IO.File.OpenRead("pusobnost_v_agendach.full.json"))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    using (var reader = new JsonTextReader(streamReader))
                    {
                        var serializer = new JsonSerializer();

                        raw = serializer.Deserialize<pusobnost_v_agendach.Pusobnosti.Raw>(reader);
                    }
                }
            }

            //var raw = Newtonsoft.Json.JsonConvert.DeserializeObject<pusobnost_v_agendach.Pusobnosti.Raw>(System.IO.File.ReadAllText("pusobnost_v_agendach.json"));
            //var agendyRaw2 = Newtonsoft.Json.Linq.JObject.Parse(json);

            var dsPrac = HlidacStatu.Api.V2.Dataset.Typed.Dataset<OVM.PracovištěOvm>.OpenDataset(apiKey, DatasetNamePracoviste);


            Devmasters.Batch.Manager.DoActionForAll<pusobnost_v_agendach.Pusobnosti.Raw.PoložkyStruct>(raw.Položky

                ,
          pr =>
          {
              pusobnost_v_agendach.Pusobnosti.Pusobnost p = new pusobnost_v_agendach.Pusobnosti.Pusobnost();
              p.Id = pr.Id;
              p.Cinnosti = pr.Činnosti;
              string ico = "";
              if (!string.IsNullOrEmpty(pr.Registrace.Ovm))
              {
                  //      "ovm" : "orgán-veřejné-moci/00263958",
                  ico = Devmasters.RegexUtil.GetRegexGroupValue(pr.Registrace.Ovm, ".*/(?<ic>\\d*)", "ic");
              }
              else
              {
                  //      "spuú" : "soukromoprávní-uživatel-údajů/27085031.9999",
                  ico = Devmasters.RegexUtil.GetRegexGroupValue(pr.Registrace.Spuú, ".*/(?<ic>\\d*)", "ic");
              }
              p.SubjektId = ico;
              p.Registrace = new pusobnost_v_agendach.Pusobnosti.Registrace()
              {
                  Agenda = pr.Registrace.Agenda,
                  Datum = pr.Registrace.Datum?.DateTime,
                  Ovm = pr.Registrace.Ovm,
                  Spuu = pr.Registrace.Spuú
              };

              List<pusobnost_v_agendach.Pusobnosti.Sluzby> sluz = new List<pusobnost_v_agendach.Pusobnosti.Sluzby>();
              if (pr.Služby != null)
              {
                  foreach (var m in pr.Služby)
                  {
                      var sl = new pusobnost_v_agendach.Pusobnosti.Sluzby();

                      sl.DuvodNeuvedeniPracovist = m.DůvodNeuvedeníPracovišť;
                      sl.Sluzba = new pusobnost_v_agendach.Pusobnosti.Sluzby.SluzbaStruct()
                      {
                          Agenda = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.Agenda,
                          Cinnosti = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.Činnosti,
                          Nazev = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.Název?.Cs,
                          Popis = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.Popis?.Cs,
                          SluzbaId = m.Služba,
                          TypSluzby = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.TypSlužby,
                      };
                      if (m.Služba != null)
                      {
                          var uk = sluzby.Get().FirstOrDefault(n => n.Id == m.Služba)?.Úkony;
                          if (uk != null)
                              sl.Sluzba.Ukony = uk.Select(n => ukony.FirstOrDefault(nn => nn.Id == n.Id))
                                                .ToArray();
                      }
                      if (m.Pracoviště != null)
                      {
                          List<pusobnost_v_agendach.Pusobnosti.Sluzby.PracovisteStruct> pracov = new List<pusobnost_v_agendach.Pusobnosti.Sluzby.PracovisteStruct>();

                          foreach (var pn in m.Pracoviště)
                          {
                              try
                              {

                                  //var prac = dsPrac.GetItem(pn);
                                  var prac = pracoviste.FirstOrDefault(m1 => m1.Id == pn);
                                  if (prac != null)
                                  {
                                      pusobnost_v_agendach.Pusobnosti.Sluzby.PracovisteStruct ps = new pusobnost_v_agendach.Pusobnosti.Sluzby.PracovisteStruct()
                                      {
                                          Adresa = prac.AdresaPrTxt,
                                          AdresniMistoKod = prac.AdresaPr?.Kód,
                                          PracovisteId = prac.Id,
                                          Stat = prac.StátPr
                                      };
                                      pracov.Add(ps);
                                  }
                              }
                              catch (Exception) //not found
                              {

                                  throw;
                              }
                          }
                          sl.Pracoviste = pracov.ToArray();
                      }
                      sluz.Add(sl);

                  }
              }
              p.Sluzby = sluz.ToArray();

              p.Type = pr.Type;

              //ds.AddOrUpdateItem(p, HlidacStatu.Api.V2.Dataset.Typed.ItemInsertMode.rewrite); //.AddOrRewriteItem(ag);

              return new Devmasters.Batch.ActionOutputData();
          }, Devmasters.Batch.Manager.DefaultOutputWriter, Devmasters.Batch.Manager.DefaultProgressWriter,
          !System.Diagnostics.Debugger.IsAttached,
          maxDegreeOfParallelism: 2, prefix: "save pusobnost ");


        }

    }
}
