using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using HlidacStatu.Entities;
using HlidacStatu.Entities.XSD;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;
using HlidacStatu.Util.Cache;

using Nest;

namespace HlidacStatu.Extensions
{
    public static class SmlouvaPrilohaExtension
    {
        private static volatile FileCacheManager prilohaTblsCacheManager
            = FileCacheManager.GetSafeInstance("SmlouvyPrilohyTblsFormat",
                smlouvaKeyId => getTablesFromDocument(smlouvaKeyId),
                TimeSpan.FromDays(365 * 10)); //10 years

        private static byte[] getTablesFromDocument(KeyAndId smlouvaKeyId)
        {
            var key = smlouvaKeyId.ValueForData.Split("|");


            Smlouva s = SmlouvaRepo.Load(key[0]);

            Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => (m.UniqueHash()) == key[1]);

            if (p == null)
                return null;
            if (string.IsNullOrEmpty(p.nazevSouboru))
            {
                Util.Consts.Logger.Error($"smlouva {key[0]} soubor {p.UniqueHash()} doesn't have nazevSouboru");
                return null;
            }
            if (p.nazevSouboru.ToLower().EndsWith("pdf"))
            {
                try
                {
                    Lib.Data.External.Tables.Result[] myRes = HlidacStatu.Lib.Data.External.Tables.PDF.GetMaxTablesFromPDFAsync(
                        p.odkaz, HlidacStatu.Lib.Data.External.Tables.Camelot.CamelotResult.Formats.JSON).Result;

                    return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(myRes));

                }
                catch (Exception e)
                {
                    Util.Consts.Logger.Error($"Stemmer returned incomplete json for {smlouvaKeyId.ValueForData}", e);
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
            var keyval = s.Id + "|" + hash;
            var key = new KeyAndId() { ValueForData = keyval, CacheNameOnDisk = $"prlh_tblsJSON_{keyval}" };
            if (forceUpdate)
            {
                prilohaTblsCacheManager.Delete(key);
            }

            var data = prilohaTblsCacheManager.Get(key);
            if (data == null)
                return null;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Lib.Data.External.Tables.Camelot.CamelotResult[]>(Encoding.UTF8.GetString(data));
        }
    }
}