using Devmasters.Cache.File;

using HlidacStatu.Entities;
using HlidacStatu.Repositories;


using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class SmlouvaPrilohaExtension
    {


        public static async Task<DocTables.Result[]> ParseTablesFromDocumentAsync(string smlouvaId, string prilohaId)
        {

            Smlouva s = await SmlouvaRepo.LoadAsync(smlouvaId);

            Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => m.UniqueHash() == prilohaId);

            if (p == null)
                return null;
            if (string.IsNullOrEmpty(p.nazevSouboru))
            {
                Util.Consts.Logger.Warning($"smlouva {smlouvaId} soubor {p.UniqueHash()} doesn't have nazevSouboru");
                return null;
            }
            return await ParseTablesFromDocumentAsync(s, p);
        }

        public static async Task<DocTables.Result[]> ParseTablesFromDocumentAsync(Smlouva smlouva, Smlouva.Priloha priloha, bool throwException = false)
        {
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            try
            {
                DocTables.Result[] myRes = null;
                if (smlouva.znepristupnenaSmlouva()==false)
                    myRes = PDF.GetMaxTablesFromPDFAsync(
                        priloha.LocalCopyUrl(smlouva.Id, true,null, null), 
                            Camelot.CamelotResult.Formats.JSON).Result;

                if (myRes == null)
                    myRes = PDF.GetMaxTablesFromPDFAsync(
                        priloha.LocalCopyUrl(smlouva.Id, true, secret: Devmasters.Config.GetWebConfigValue("LocalPrilohaUniversalSecret")),
                        Camelot.CamelotResult.Formats.JSON).Result;

                sw.Stop();
                Util.Consts.Logger.Debug($"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} done in {sw.ElapsedMilliseconds}ms, found {myRes?.Sum(m => m.Tables?.Length ?? 0)} tables");

                return myRes;

            }
            catch (AggregateException age)
            {
                sw.Stop();
                if (age.InnerExceptions?.Count > 0)
                {
                    foreach (var e in age.InnerExceptions)
                    {
                        Util.Consts.Logger.Error($"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms  {e.ToString()}", e);
                    }
                }
                else
                    Util.Consts.Logger.Error($"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms {age.ToString()}", age);
                if (throwException)
                    throw;
                else
                    return null;
            }
            catch (Exception e)
            {
                sw.Stop();
                Util.Consts.Logger.Error($"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} error GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms, {e.ToString()}", e);
                if (throwException)
                    throw;
                else
                    return null;
            }

            return null;
        }

        public static DocTables.Result[] GetTablesFromPriloha(Smlouva s, Smlouva.Priloha p)
        {
            if (s == null || p == null)
                return null;

            DocTables data = DocTablesRepo.GetAsync(s.Id,p.UniqueHash())
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            if (data == null)
                return null;

            return data.Tables;
        }
    }
}