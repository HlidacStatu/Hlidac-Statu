using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.Searching;

using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Repositories
{
    public static partial class InsolvenceRepo
    {

        static string[] defaultRolesWithoutLimitation = new string[] { }; //"Admin", "novinar" };

        public static bool IsLimitedAccess(System.Security.Principal.IPrincipal user, string[]? validRoles = null)
        {
            validRoles ??= defaultRolesWithoutLimitation;
            if (user?.Identity?.IsAuthenticated == true)
            {
                if (validRoles.Count() == 0)
                    return false;

                foreach (var role in validRoles)
                {
                    if (user.IsInRole(role.Trim()))
                        return false;
                }
                return true;
            }
            return true;

        }

        public static async Task<InsolvenceDetail> LoadFromEsAsync(string id, bool includeDocumentsPlainText, bool limitedView)
        {
            var client = await Manager.GetESClient_InsolvenceAsync();
            var spisovaZnacka = ParseId(id);

            try
            {
                var rizeni = includeDocumentsPlainText
                    ? await client.GetAsync<Rizeni>(spisovaZnacka)
                    : await client.GetAsync<Rizeni>(spisovaZnacka, s => s
                            .SourceExcludes("dokumenty.plainText")
                    //.SourceExclude("")
                    );

                if (rizeni.Found)
                {
                    var r = rizeni.Source;
                    if (limitedView && r.OnRadar == false)
                        return null;

                    r.IsFullRecord = includeDocumentsPlainText;
                    return new InsolvenceDetail
                    {
                        Rizeni = r,
                    };
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                // TODO: handle error
                throw;
            }
        }


        public static async Task SaveRizeniAsync(Rizeni r) => await RizeniRepo.SaveAsync(r);

        public static async Task<DokumentSeSpisovouZnackou> LoadDokumentAsync(string id, bool limitedView)
        {
            var client = await Manager.GetESClient_InsolvenceAsync();

            try
            {
                ISearchResponse<Rizeni> data = await client.SearchAsync<Rizeni>(s => s
                    .Source(sr => sr.Includes(r => r.Fields("dokumenty.*").Fields("spisovaZnacka")))
                    .Query(q => q.Match(m => m.Field("dokumenty.id").Query(id)))); //TODO

                if (data.IsValid == false)
                    throw new ApplicationException(data.ServerError?.ToString() ?? "");

                if (data.Total == 0)
                    return null;

                return data.Hits.Select(h => new DokumentSeSpisovouZnackou
                {
                    SpisovaZnacka = h.Source.SpisovaZnacka,
                    UrlId = h.Source.NormalizedId(),
                    Dokument = h.Source.Dokumenty.Single(d => d.Id == id),
                    Rizeni = h.Source
                }).First();
            }
            catch (Exception)
            {
                // TODO: handle error
                throw;
            }
        }

        public static Task<InsolvenceSearchResult> NewFirmyVInsolvenciAsync(int count, bool limitedView) 
            => NewSubjektVInsolvenciAsync(count, "P", limitedView);

        public static Task<InsolvenceSearchResult> NewOsobyVInsolvenciAsync(int count, bool limitedView) 
            => NewSubjektVInsolvenciAsync(count, "F", limitedView);

        private static Task<InsolvenceSearchResult> NewSubjektVInsolvenciAsync(int count, string typ, bool limitedView)
        {
            return InsolvenceRepo.Searching.SimpleSearchAsync("dluznici.typ:" + typ, 1, count,
                (int)InsolvenceSearchResult.InsolvenceOrderResult.DateAddedDesc, false, limitedView, null);
        }

        public static IEnumerable<string> AllIdsFromDb()
        {
            //return AllIdsFromDB(null);
            using (var db = new Lib.Db.Insolvence.InsolvenceEntities())
            {
                return db.Rizeni.AsNoTracking().Select(m => m.SpisovaZnacka)
                    .ToArray(); //force query
            }
        }

        public static async Task<IEnumerable<string>> AllIdsFromEsAsync(Action<string> outputWriter = null,
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null)
        {
            Func<int, int, Task<ISearchResponse<Rizeni>>> searchFunc = async (size, page) =>
            {
                var client = await Manager.GetESClient_InsolvenceAsync();
                return await client.SearchAsync<Rizeni>(a => a
                    .Size(size)
                    .Source(false)
                    .From(page * size)
                    .Query(q => q.MatchAll())
                    .Scroll("1m")
                    .TrackTotalHits(page * size == 0 ? true : (bool?)null)
                );
            };

            List<string> ids = new List<string>();
            await Tools.DoActionForQueryAsync<Rizeni>(await Manager.GetESClient_InsolvenceAsync(),
                searchFunc, (hit, param) =>
                {
                    ids.Add(hit.Id);
                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = false, Log = null };
                }, null, outputWriter, progressWriter, false, prefix: "get_id_Insolvence ", blockSize: 10);
            return ids;
        }

        private static QueryContainer SpisovaZnackaQuery(string spisovaZnacka)
        {
            return new QueryContainerDescriptor<Osoba>().QueryString(qs =>
                qs.Query($"spisovaZnacka:\"{spisovaZnacka}\""));
        }

        private static string ParseId(string id)
        {
            return id.Replace("_", " ").Replace("-", "/");
        }
    }
}