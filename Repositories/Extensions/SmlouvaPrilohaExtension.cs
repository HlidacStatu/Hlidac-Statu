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
                smlouvaKeyId => getTablesFromPDFViaCamelot(smlouvaKeyId),
                TimeSpan.FromDays(365 * 10)); //10 years

        private static byte[] getTablesFromPDFViaCamelot(KeyAndId smlouvaKeyId)
        {
            var key = smlouvaKeyId.ValueForData.Split("|");


            Smlouva s = SmlouvaRepo.Load(key[0]);
            
            Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => (m.hash?.Value ?? Devmasters.Crypto.Hash.ComputeHashToHex(m.odkaz ?? "")) == key[1]);

            if (p == null)
                return null;

            if (p.nazevSouboru.ToLower().EndsWith("pdf") == false)
                return null;

            try
            {
                Lib.Data.External.Camelot.CamelotResult[] myRes = HlidacStatu.Lib.Data.External.Camelot.Client.GetMaxTablesFromPDFAsync(
                    p.odkaz, HlidacStatu.Lib.Data.External.Camelot.CamelotResult.Formats.HTML).Result;

                return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(myRes));

            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error($"Stemmer returned incomplete json for {smlouvaKeyId.ValueForData}", e);
                throw;
            }

        }

        public static Lib.Data.External.Camelot.CamelotResult[] GetTablesFromPriloha(Smlouva s,  Smlouva.Priloha p, bool forceUpdate = false)
        {
            if (s == null || p == null)
                return null;


            string hash = p.hash?.Value ?? Devmasters.Crypto.Hash.ComputeHashToHex(p.odkaz?? "");
            var keyval = s.Id + "|" + hash;
            var key = new KeyAndId() { ValueForData = keyval, CacheNameOnDisk = $"priloha_tbls_{keyval}" };
            if (forceUpdate)
            {
                prilohaTblsCacheManager.Delete(key);
            }

            var data = prilohaTblsCacheManager.Get(key);
            if (data == null)
                return null;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Lib.Data.External.Camelot.CamelotResult[]>(Encoding.UTF8.GetString(data));
        }
    }
}