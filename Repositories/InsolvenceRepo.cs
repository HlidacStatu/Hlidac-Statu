using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;

using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class InsolvenceRepo
    {
        public static InsolvenceDetail LoadFromES(string id, bool includeDocumentsPlainText, bool limitedView)
        {
            var client = Manager.GetESClient_Insolvence();
            var spisovaZnacka = ParseId(id);

            try
            {
                var rizeni = includeDocumentsPlainText
                    ? client.Get<Rizeni>(spisovaZnacka)
                    : client.Get<Rizeni>(spisovaZnacka, s => s
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


        public static void SaveRizeni(Rizeni r)
        {
            RizeniRepo.SaveAsync(r);
        }

        public static DokumentSeSpisovouZnackou LoadDokument(string id, bool limitedView)
        {
            var client = Manager.GetESClient_Insolvence();

            try
            {
                var data = client.Search<Rizeni>(s => s
                    .Source(sr => sr.Includes(r => r.Fields("dokumenty.*").Fields("spisovaZnacka")))
                    .Query(q => q.Match(m => m.Field("dokumenty.id").Query(id)))); //TODO

                if (data.IsValid == false)
                    throw new ApplicationException(data.ServerError?.ToString() ?? "");

                if (data.Total == 0)
                    return null;

                return data.Hits.Select(h => new DokumentSeSpisovouZnackou
                {
                    SpisovaZnacka = h.Source.SpisovaZnacka,
                    UrlId = h.Source.UrlId(),
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

        public static InsolvenceSearchResult NewFirmyVInsolvenci(int count, bool limitedView)
        {
            return NewSubjektVInsolvenci(count, "P", limitedView);
        }

        public static InsolvenceSearchResult NewOsobyVInsolvenci(int count, bool limitedView)
        {
            return NewSubjektVInsolvenci(count, "F", limitedView);
        }

        private static InsolvenceSearchResult NewSubjektVInsolvenci(int count, string typ, bool limitedView)
        {
            var rs = InsolvenceRepo.Searching.SimpleSearch("dluznici.typ:" + typ, 1, count,
                (int)InsolvenceSearchResult.InsolvenceOrderResult.DateAddedDesc, false, limitedView, null);

            return rs;

        }

        public static IEnumerable<string> AllIdsFromDB()
        {
            //return AllIdsFromDB(null);
            using (var db = new Lib.Db.Insolvence.InsolvenceEntities())
            {
                return db.Rizeni.AsNoTracking().Select(m => m.SpisovaZnacka)
                    .ToArray(); //force query
            }
        }

        public static IEnumerable<string> AllIdsFromES(Action<string> outputWriter = null,
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null)
        {
            Func<int, int, ISearchResponse<Rizeni>> searchFunc = (size, page) =>
            {
                return Manager.GetESClient_Insolvence().Search<Rizeni>(a => a
                    .Size(size)
                    .Source(false)
                    .From(page * size)
                    .Query(q => q.MatchAll())
                    .Scroll("1m")
                    .TrackTotalHits(page * size == 0 ? true : (bool?)null)
                );
            };

            List<string> ids = new List<string>();
            Tools.DoActionForQuery<Rizeni>(Manager.GetESClient_Insolvence(),
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