using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class SmlouvaPrilohaExtension
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(SmlouvaPrilohaExtension));

        public static async Task<HlidacStatu.DS.Api.TablesInDoc.Result[]> ParseTablesFromDocumentAsync(string smlouvaId, string prilohaId)
        {

            Smlouva s = await SmlouvaRepo.LoadAsync(smlouvaId);

            Smlouva.Priloha p = s?.Prilohy?.FirstOrDefault(m => m.UniqueHash() == prilohaId);

            if (p == null)
                return null;
            if (string.IsNullOrEmpty(p.nazevSouboru))
            {
                _logger.Warning($"smlouva {smlouvaId} soubor {p.UniqueHash()} doesn't have nazevSouboru");
                return null;
            }
            return await ParseTablesFromDocumentAsync(s, p);
        }

        public static async Task<HlidacStatu.DS.Api.TablesInDoc.Result[]> ParseTablesFromDocumentAsync(Smlouva smlouva, Smlouva.Priloha priloha, bool throwException = false)
        {
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            try
            {
                HlidacStatu.DS.Api.TablesInDoc.Result[] myRes = null;
                if (smlouva.znepristupnenaSmlouva()==false)
                    myRes = PDF.GetMaxTablesFromPDFAsync(
                        priloha.LocalCopyUrl(smlouva.Id, true,null, null), 
                            HlidacStatu.DS.Api.TablesInDoc.Formats.JSON).Result;

                if (myRes == null)
                    myRes = PDF.GetMaxTablesFromPDFAsync(
                        priloha.LocalCopyUrl(smlouva.Id, true, secret: Devmasters.Config.GetWebConfigValue("LocalPrilohaUniversalSecret")),
                        HlidacStatu.DS.Api.TablesInDoc.Formats.JSON).Result;

                sw.Stop();
                _logger.Debug($"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} done in {sw.ElapsedMilliseconds}ms, found {myRes?.Sum(m => m.Tables?.Length ?? 0)} tables");

                return myRes;

            }
            catch (AggregateException age)
            {
                sw.Stop();
                if (age.InnerExceptions?.Count > 0)
                {
                    foreach (var e in age.InnerExceptions)
                    {
                        _logger.Error(e, $"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms  {e.ToString()}");
                    }
                }
                else
                    _logger.Error(age, $"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} errors GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms {age.ToString()}");
                if (throwException)
                    throw;
                else
                    return null;
            }
            catch (Exception e)
            {
                sw.Stop();
                _logger.Error(e, $"smlouva {smlouva.Id} soubor {priloha.UniqueHash()} error GetMaxTablesFromPDFAsync in {sw.ElapsedMilliseconds}ms, {e.ToString()}");
                if (throwException)
                    throw;
                else
                    return null;
            }

            return null;
        }

        public static bool HasTablesFromPriloha(Smlouva s, Smlouva.Priloha p)
        {
            if (s == null || p == null)
                return false;

            return DocTablesRepo.ExistsAsync(s.Id, p.UniqueHash())
                    .ConfigureAwait(false).GetAwaiter().GetResult();

        }
        public static HlidacStatu.DS.Api.TablesInDoc.Result[] GetTablesFromPriloha(Smlouva s, Smlouva.Priloha p)
        {
            if (s == null || p == null)
                return null;

            DocTables data = DocTablesRepo.GetAsync(s.Id,p.UniqueHash())
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            if (data == null)
                return null;

            return data.Tables;
        }
        public static HlidacStatu.Entities.DocTables GetTablesInDocStructure(Smlouva s, Smlouva.Priloha p)
        {
            if (s == null || p == null)
                return null;

            DocTables data = DocTablesRepo.GetAsync(s.Id, p.UniqueHash())
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            if (data == null)
                return null;

            return data;
        }
    }
}