using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Util.Cache;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class SmlouvaPrilohaExtension
    {
        
        private static volatile MinioCacheManager<Lib.Data.External.Tables.Result[],KeyAndId> prilohaTblsMinioCacheManager
        = MinioCacheManager<Lib.Data.External.Tables.Result[],KeyAndId>.GetSafeInstance(
            "SmlouvyPrilohyTbls/",
            smlouvaKeyId => getTablesFromDocumentAsync(smlouvaKeyId).ConfigureAwait(false).GetAwaiter().GetResult(),
            TimeSpan.Zero,
            new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
            Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            key=>key.CacheNameOnDisk
            );

        private static async Task<Result[]> getTablesFromDocumentAsync(KeyAndId smlouvaKeyId)
        {
            var key = smlouvaKeyId.ValueForData.Split("/");


            Smlouva s = await SmlouvaRepo.LoadAsync(key[0]);

            Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => (m.UniqueHash()) == key[1]);

            if (p == null)
                return null;
            if (string.IsNullOrEmpty(p.nazevSouboru))
            {
                Util.Consts.Logger.Warning($"smlouva {key[0]} soubor {p.UniqueHash()} doesn't have nazevSouboru");
                return null;
            }
            if (p.nazevSouboru.ToLower().EndsWith("pdf"))
            {
                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                sw.Start();
                try
                {
                    Lib.Data.External.Tables.Result[] myRes = HlidacStatu.Lib.Data.External.Tables.PDF.GetMaxTablesFromPDFAsync(
                        p.odkaz, HlidacStatu.Lib.Data.External.Tables.Camelot.CamelotResult.Formats.JSON).Result;
                    if (myRes==null)
                        myRes = HlidacStatu.Lib.Data.External.Tables.PDF.GetMaxTablesFromPDFAsync(
                            p.LocalCopyUrl(s.Id,secret: Devmasters.Config.GetWebConfigValue("LocalPrilohaUniversalSecret")), 
                            Camelot.CamelotResult.Formats.JSON).Result;

                    sw.Stop();
                    Util.Consts.Logger.Debug($"smlouva {key[0]} soubor {p.UniqueHash()} done in {sw.ElapsedMilliseconds}ms, found {myRes?.Sum(m => m.Tables?.Length ?? 0)} tables");

                    return myRes;

                }
                catch (AggregateException age)
                {
                    sw.Stop();
                    if (age.InnerExceptions?.Count >0)
                    {
                        foreach (var e in age.InnerExceptions)
                        {
                            Util.Consts.Logger.Error($"smlouva {key[0]} soubor {p.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms  {e.ToString()}", e);
                        }
                    }
                    else
                        Util.Consts.Logger.Error($"smlouva {key[0]} soubor {p.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms {age.ToString()}", age);

                    throw;

                }
                catch (Exception e)
                {
                    sw.Stop();
                    Util.Consts.Logger.Error($"smlouva {key[0]} soubor {p.UniqueHash()} error GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms, {e.ToString()}", e);
                    throw;
                }
            }
            else
            {

            }

            return null;
        }

        public static Lib.Data.External.Tables.Result[] GetTablesFromPriloha(Smlouva s, Smlouva.Priloha p, bool forceUpdate = false)
        {
            if (s == null || p == null)
                return null;


            string hash = p.UniqueHash();
            var keyval = s.Id + "/" + hash;
            var key = new KeyAndId() { ValueForData = keyval, CacheNameOnDisk = $"{keyval}" };
            if (forceUpdate)
            {
                prilohaTblsMinioCacheManager.Delete(key);
            }

            var data = prilohaTblsMinioCacheManager.Get(key);
            if (data == null)
                return null;

            return data;
        }
    }
}