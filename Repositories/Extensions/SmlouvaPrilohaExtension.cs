// using HlidacStatu.Entities;
// using HlidacStatu.Repositories;
// using HlidacStatu.Util.Cache;
//
// using System;
// using System.Linq;
// using System.Text;
//
// namespace HlidacStatu.Extensions
// {
//     public static class SmlouvaPrilohaExtension
//     {
//         //private static volatile FileCacheManager prilohaTblsCacheManager
//         //    = FileCacheManager.GetSafeInstance("SmlouvyPrilohyTblsFormat",
//         //        smlouvaKeyId => getTablesFromDocumentOld(smlouvaKeyId),
//         //        TimeSpan.FromDays(365 * 10)); //10 years
//
//         private static volatile MinioCacheManager<Lib.Data.External.Tables.Result[],KeyAndId> prilohaTblsMinioCacheManager
//         = MinioCacheManager<Lib.Data.External.Tables.Result[],KeyAndId>.GetSafeInstance(
//             "SmlouvyPrilohyTbls",
//             smlouvaKeyId => getTablesFromDocument(smlouvaKeyId),
//             TimeSpan.Zero,
//             new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
//             Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
//             Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
//             Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
//             key=>key.CacheNameOnDisk
//             );
//
//         //Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(
//
//         private static byte[] getTablesFromDocumentOld(KeyAndId smlouvaKeyId)
//         {
//             return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(getTablesFromDocument(smlouvaKeyId)));
//         }
//         private static Lib.Data.External.Tables.Result[] getTablesFromDocument(KeyAndId smlouvaKeyId)
//         {
//             var key = smlouvaKeyId.ValueForData.Split("|");
//
//
//             Smlouva s = SmlouvaRepo.Load(key[0]);
//
//             Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => (m.UniqueHash()) == key[1]);
//
//             if (p == null)
//                 return null;
//             if (string.IsNullOrEmpty(p.nazevSouboru))
//             {
//                 Util.Consts.Logger.Error($"smlouva {key[0]} soubor {p.UniqueHash()} doesn't have nazevSouboru");
//                 return null;
//             }
//             if (p.nazevSouboru.ToLower().EndsWith("pdf"))
//             {
//                 try
//                 {
//                     Lib.Data.External.Tables.Result[] myRes = HlidacStatu.Lib.Data.External.Tables.PDF.GetMaxTablesFromPDFAsync(
//                         p.odkaz, HlidacStatu.Lib.Data.External.Tables.Camelot.CamelotResult.Formats.JSON).Result;
//
//                     return myRes;
//
//                 }
//                 catch (Exception e)
//                 {
//                     Util.Consts.Logger.Error($"Stemmer returned incomplete json for {smlouvaKeyId.ValueForData}", e);
//                     throw;
//                 }
//             }
//             else
//             {
//
//             }
//
//             return null;
//
//
//         }
//
//         public static Lib.Data.External.Tables.Result[] GetTablesFromPriloha(Smlouva s, Smlouva.Priloha p, bool forceUpdate = false)
//         {
//             if (s == null || p == null)
//                 return null;
//
//
//             string hash = p.UniqueHash();
//             var keyval = s.Id + "|" + hash;
//             var key = new KeyAndId() { ValueForData = keyval, CacheNameOnDisk = $"prlh_tblsJSON_{keyval}" };
//             if (forceUpdate)
//             {
//                 prilohaTblsMinioCacheManager.Delete(key);
//             }
//
//             var data = prilohaTblsMinioCacheManager.Get(key);
//             if (data == null)
//                 return null;
//
//             return data;
//         }
//     }
// }