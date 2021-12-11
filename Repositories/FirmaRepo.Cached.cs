using Devmasters.Cache;
using Devmasters.Cache.LocalMemory;

using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Util;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        private static string AppDataPath = null;
        public static HashSet<string> VsechnyStatniMestskeFirmy = new HashSet<string>();
        public static HashSet<string> VsechnyStatniMestskeFirmy25percs = new HashSet<string>();
        public static HashSet<string> Urady_OVM = new HashSet<string>();
        public static XDocument DatoveSchranky = null;
        public static XNamespace DatoveSchrankyNS = null;
        public static BaseCache<IEnumerable<Firma>> MinisterstvaCache = null;
        public static BaseCache<IEnumerable<Firma>> VysokeSkolyCache = null;
        public static BaseCache<IEnumerable<Firma>> KrajskeUradyCache = null;
        public static BaseCache<IEnumerable<Firma>> ManualChoosenCache = null;
        public static BaseCache<IEnumerable<Firma>> StatutarniMestaAllCache = null;
        public static BaseCache<IEnumerable<Firma>> PrahaManualCache = null;
        public static BaseCache<IEnumerable<Firma>> OrganizacniSlozkyStatuCache = null;
        public static Dictionary<string, string[]> MestaPodleKraju = null;
        public static string[] ObceIII_DS = null;

        public static
            Devmasters.Cache.File.Cache<System.Collections.Concurrent.ConcurrentDictionary<string, string[]>>
            FirmyNazvyOnlyAscii = null;

        static FirmaRepo()
        {
            AppDataPath = Init.WebAppDataPath;
            if (string.IsNullOrEmpty(AppDataPath))
            {
                throw new ArgumentNullException("App_Data_Path");
            }


            Consts.Logger.Info("Static data - Mestske_Firmy ");
            VsechnyStatniMestskeFirmy = FirmaVlastnenaStatemRepo.IcaStatnichFirem().ToHashSet();
            VsechnyStatniMestskeFirmy.Add("60193913"); //Pražská energetika
            foreach (var ic in
                "25054040,48592307,27376516,06714366,25677063,27234835,28880757,28537319,29202311,28080378,28923405,27831248,27966216,02065801,06532438,44794274"
                    .Split(','))
                VsechnyStatniMestskeFirmy.Add(ic); //podrizenky Pražská energetika

            VsechnyStatniMestskeFirmy25percs = FirmaVlastnenaStatemRepo.IcaStatnichFirem(25).ToHashSet();
            VsechnyStatniMestskeFirmy25percs.Add("60193913"); //Pražská energetika
            foreach (var ic in
                "25054040,48592307,27376516,06714366,25677063,27234835,28880757,28537319,29202311,28080378,28923405,27831248,27966216,02065801,06532438,44794274"
                    .Split(','))
                VsechnyStatniMestskeFirmy.Add(ic); //podrizenky Pražská energetika


            string ds_ovm = AppDataPath + "DS_OVM.xml";

            //zdroj https://www.czechpoint.cz/public/vyvojari/otevrena-data/
            Consts.Logger.Info("Static data - DatoveSchranky ");
            using (var xml = File.OpenText(ds_ovm))
            {
                DatoveSchranky = XDocument.Load(xml);
            }

            DatoveSchrankyNS = DatoveSchranky.Root.Name.Namespace;

            foreach (var ico in DatoveSchranky
                .Descendants(DatoveSchrankyNS + "Subjekt")
                .Where(m => m.Element(DatoveSchrankyNS + "TypDS")?.Value?.StartsWith("OVM") == true)
                .Select(m => m.Element(DatoveSchrankyNS + "ICO")?.Value ?? "")
                .Where(i => !string.IsNullOrEmpty(i))
                .Select(i => ParseTools.MerkIcoToICO(i))
            )
            {
                Urady_OVM.Add(ico);
            }

            //dalsi vyjimky
            Urady_OVM.Add("00832227"); //Euroregion Neisse - Nisa - Nysa


            Consts.Logger.Info("Static data - MinisterstvaCache");
            MinisterstvaCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.Ministerstva",
                (o) =>
                {
                    return DatoveSchranky
                            .Descendants(DatoveSchrankyNS + "Subjekt")
                            .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "4")
                            .OrderBy(m => m.Element(DatoveSchrankyNS + "Nazev").Value)
                            .Select(m => m.Element(DatoveSchrankyNS + "IdDS").Value)
                            .Select(ds => Firmy.GetByDS(ds))
                            .ToArray()
                        ;
                });

            VysokeSkolyCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.VysokeSkoly",
                (o) =>
                {
                    string[] icos = new string[]
                    {
                        "61384984", "60461446", "60460709", "68407700", "62156462", "60076658", "00216224",
                        "62156489", "61988987", "47813059", "46747885", "62690094", "44555601", "00216208",
                        "61989592", "00216275", "70883521", "62157124", "61989100", "61384399", "60461373",
                        "71226401", "75081431", "60461071", "00216305", "49777513", "48135445"
                    };
                    //string[] ds = new string[] { "hkraife" };

                    return icos.Select(i => Firmy.Get(i))
                        .OrderBy(or => or.Jmeno);
                });


            KrajskeUradyCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.KrajskeUrady",
                (o) =>
                {
                    return DatoveSchranky
                        .Descendants(DatoveSchrankyNS + "Subjekt")
                        .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "3")
                        .OrderBy(m => m.Element(DatoveSchrankyNS + "Nazev").Value)
                        .Select(m => m.Element(DatoveSchrankyNS + "IdDS").Value)
                        .Select(ds => Firmy.GetByDS(ds));
                });

            ManualChoosenCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6),
                "StatData.ManualChoosen",
                (o) =>
                {
                    string[] icos = new string[]
                    {
                        "72054506", "47114983", "61459445", "44848943", "69797111",
                        "25291581", "41197518", "70890021", "47672234", "28244532", "00020729",
                        "01312774", "70994234", "60498021", "63080249", "61388971", "48136450", "66000769",
                        //krajske silnicni spravy
                        "70947023", "00090450", "00064785", "00073679", "70971641", "70932581", "00065374",
                        "00074870",
                        "00075779", "00066621", "00085031", "72053119", "00075957", "00075477", "00066001",
                        "00076520", "00080837"
                    };
                    string[] ds = new string[]
                    {
                        "trfaa33", "rnaadje", "weeab8c", "p9iwj4f", "zjq4rhz", "e8jcfsn",
                        "4iqaa3x", "yypyq58", "5smaetu", "gn5rgc9", "wwjaa4f", "kccaa9t",
                        "ag5uunk", "hkrkpwn"
                    };

                    return icos.Select(i => Firmy.Get(i))
                        .Union(ds.Select(d => Firmy.GetByDS(d)))
                        .OrderBy(or => or.Jmeno);
                });


            var obceIII = DatoveSchranky
                .Descendants(DatoveSchrankyNS + "Subjekt")
                .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "7")
                .Where(m => m.Element(DatoveSchrankyNS + "PrimarniOvm")?.Value == "Ano");

            MestaPodleKraju = DatoveSchranky
                .Descendants(DatoveSchrankyNS + "Subjekt")
                .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "8")
                .Where(m => m.Element(DatoveSchrankyNS + "PrimarniOvm")?.Value == "Ano")
                .Union(obceIII)
                .GroupBy(k =>
                        k.Element(DatoveSchrankyNS + "AdresaUradu")?.Element(DatoveSchrankyNS + "KrajNazev")?.Value ??
                        "Královéhradecký" //chybejici u Dvora kraloveho
                                          //, v => new { DS = v.Element(DatoveSchrankyNS + "IdDS").Value, Nazev = v.Element(DatoveSchrankyNS + "Nazev").Value }
                    , v => v.Element(DatoveSchrankyNS + "IdDS").Value
                )
                .ToDictionary(k => k.Key, v => v.ToArray());


            ObceIII_DS = obceIII
                .OrderBy(m => m.Element(DatoveSchrankyNS + "Nazev").Value)
                .Select(m => m.Element(DatoveSchrankyNS + "IdDS").Value)
                .ToArray();

            StatutarniMestaAllCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6),
                "StatData.StatutarniMestaAll",
                (o) =>
                {
                    return DatoveSchranky
                        .Descendants(DatoveSchrankyNS + "Subjekt")
                        .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "8")
                        .Where(m => m.Element(DatoveSchrankyNS + "PrimarniOvm")?.Value == "Ano")
                        .OrderBy(m => m.Element(DatoveSchrankyNS + "Nazev").Value)
                        .Select(m => m.Element(DatoveSchrankyNS + "IdDS").Value)
                        .Union(ObceIII_DS)
                        .Select(d => Firmy.GetByDS(d))
                        .OrderBy(or => or.Jmeno);
                });


            PrahaManualCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.PrahaManual",
                (o) =>
                {
                    var ds = new string[] { "48ia97h", "ktdeucu" };
                    return ds
                        .Select(d => Firmy.GetByDS(d))
                        .OrderBy(or => or.Jmeno);
                });

            OrganizacniSlozkyStatuCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6),
                "StatData.OrganizacniSlozkyStatu",
                (o) =>
                {
                    return DatoveSchranky
                        .Descendants(DatoveSchrankyNS + "Subjekt")
                        .Where(m => m.Element(DatoveSchrankyNS + "TypSubjektu")?.Attribute("id").Value == "11")
                        .Where(m => m.Element(DatoveSchrankyNS + "PravniForma")?.Attribute("type").Value == "325")
                        .Where(m => m.Element(DatoveSchrankyNS + "Nazev") != null)
                        .Where(m => m.Element(DatoveSchrankyNS + "PrimarniOvm")?.Value == "Ano")
                        .OrderBy(m => m.Element(DatoveSchrankyNS + "Nazev").Value)
                        .Select(m => m.Element(DatoveSchrankyNS + "IdDS").Value)
                        .Select(d => Firmy.GetByDS(d))
                        .OrderBy(or => or.Jmeno);
                });

            Util.Consts.Logger.Info("Static data - FirmyNazvyOnlyAscii");

            FirmyNazvyOnlyAscii =
                new Devmasters.Cache.File.Cache<
                    System.Collections.Concurrent.ConcurrentDictionary<string, string[]>>
                (AppDataPath, TimeSpan.Zero, "FirmyNazvyOnlyAscii",
                    (o) =>
                    {
                        Util.Consts.Logger.Info("Static data - FirmyNazvyOnlyAscii starting generation");
                        System.Collections.Concurrent.ConcurrentDictionary<string, string[]> res
                            = new System.Collections.Concurrent.ConcurrentDictionary<string, string[]>();
                        string cnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
                        using (Devmasters.PersistLib p = new Devmasters.PersistLib())
                        {
                            var reader = p.ExecuteReader(cnnStr, CommandType.Text, "select ico, jmeno from firma",
                                null);
                            while (reader.Read())
                            {
                                string ico = reader.GetString(0).Trim();
                                string name = reader.GetString(1).Trim();
                                if (Devmasters.TextUtil.IsNumeric(ico))
                                {
                                    ico = Util.ParseTools.NormalizeIco(ico);
                                    var jmenoa = Devmasters.TextUtil.RemoveDiacritics(Firma.JmenoBezKoncovky(name))
                                        .Trim().ToLower();
                                    if (!res.ContainsKey(jmenoa))
                                        res[jmenoa] = new string[] { ico };
                                    else if (!res[jmenoa].Contains(ico))
                                    {
                                        var v = res[jmenoa];
                                        res[jmenoa] = v.Union(new string[] { ico }).ToArray();
                                    }
                                }
                            }
                        }

                        Util.Consts.Logger.Info("Static data - FirmyNazvyOnlyAscii generation finished");
                        return res;
                    }
                );
        }
    }
}