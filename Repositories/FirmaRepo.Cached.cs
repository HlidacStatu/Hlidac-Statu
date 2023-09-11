using Devmasters.Cache;
using Devmasters.Cache.LocalMemory;
using Devmasters.Log;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Util;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        private static string AppDataPath = null;
        public static HashSet<string> VsechnyStatniMestskeFirmy = new HashSet<string>();
        public static HashSet<string> VsechnyStatniMestskeFirmy25percs = new HashSet<string>();
        public static HashSet<string> Urady_OVM = new HashSet<string>();
        //public static XDocument DatoveSchranky = null;
        //public static XNamespace DatoveSchrankyNS = null;
        public static BaseCache<IEnumerable<Firma>> MinisterstvaCache = null;
        public static BaseCache<IEnumerable<Firma>> VysokeSkolyCache = null;
        public static BaseCache<IEnumerable<Firma>> KrajskeUradyCache = null;
        public static BaseCache<IEnumerable<Firma>> ManualChoosenCache = null;
        public static BaseCache<IEnumerable<Firma>> ObceSRozsirenouPusobnostiCache = null;
        public static BaseCache<IEnumerable<Firma>> PrahaManualCache = null;
        public static BaseCache<IEnumerable<Firma>> OrganizacniSlozkyStatuCache = null;
        public static Dictionary<string, string[]> MestaPodleKraju = null;
        public static string[] ObceIII_DS = null;

        public static
            Devmasters.Cache.AWS_S3.Cache<System.Collections.Concurrent.ConcurrentDictionary<string, string[]>>
            FirmyNazvyOnlyAscii = null;

        static FirmaRepo()
        {
            AppDataPath = Init.WebAppDataPath;
            if (string.IsNullOrEmpty(AppDataPath))
            {
                HlidacStatu.Util.Consts.Logger.Fatal("Not defined config App_Data_Path");
                throw new ArgumentNullException("App_Data_Path");
            }

            try { 
            Consts.Logger.Info("Static data - Mestske_Firmy ");
            VsechnyStatniMestskeFirmy = FirmaVlastnenaStatemRepo.IcaStatnichFirem().ToHashSet();

            VsechnyStatniMestskeFirmy25percs = FirmaVlastnenaStatemRepo.IcaStatnichFirem(25).ToHashSet();


            
            //zdroj https://www.czechpoint.cz/public/vyvojari/otevrena-data/
            Consts.Logger.Info("Static data - DatoveSchranky ");

            foreach (var urad in OvmRepo.UradyOvm())
            {
                Urady_OVM.Add(urad.ICO);
            }
            //dalsi vyjimky
            Urady_OVM.Add("00832227"); //Euroregion Neisse - Nisa - Nysa


            Consts.Logger.Info("Static data - MinisterstvaCache");
            MinisterstvaCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.Ministerstva",
                (o) =>
                {
                    return OvmRepo.Ministerstva().Select(m => Firmy.Get(m.ICO)).ToArray(); 
                });

            VysokeSkolyCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.VysokeSkoly",
                (o) =>
                {
                    return OvmRepo.VysokeSkoly().Select(m => Firmy.Get(m.ICO)).ToArray();
                });


            KrajskeUradyCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6), "StatData.KrajskeUrady",
                (o) =>
                {
                    return OvmRepo.KrajskeUrady().Select(m => Firmy.GetByDS(m.IdDS)).ToArray();
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
            
            MestaPodleKraju = OvmRepo.ObceSRozsirenouPusobnostiPodleKraju();

            ObceIII_DS = OvmRepo.ObceIII().Select(o => o.IdDS).ToArray();

            ObceSRozsirenouPusobnostiCache = new Cache<IEnumerable<Firma>>(TimeSpan.FromHours(6),
                "StatData.StatutarniMestaAll",
                (o) =>
                {
                    return OvmRepo.ObceSRozsirenouPusobnosti().Select(m => Firmy.GetByDS(m.IdDS)).ToArray();
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
                    return OvmRepo.OrganizacniSlozkyStatu().Select(m => Firmy.GetByDS(m.IdDS)).ToArray();
                });

            Util.Consts.Logger.Info("Static data - FirmyNazvyOnlyAscii");

            FirmyNazvyOnlyAscii =
                new Devmasters.Cache.AWS_S3.Cache<
                    System.Collections.Concurrent.ConcurrentDictionary<string, string[]>>
                (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), 
                    TimeSpan.Zero, "FirmyNazvyOnlyAscii",
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
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Fatal("FirmaRepo static fatal exception", e);
                //System.IO.File.AppendAllText(@"c:\data\logs\fatal.txt", "\n\n"
                //    + DateTime.Now.ToLongDateString()
                //    + " "
                //    + DateTime.Now.ToLongTimeString()
                //    + ":"
                //    + HlidacStatu.Util.Helper.PrettyPrintException(e));
                throw;
            }
        }
    }
}