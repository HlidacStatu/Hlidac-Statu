using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Devmasters.Collections;
using Devmasters.Log;

using HlidacStatu.Analysis.Page.Area;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using Nest;

namespace HlidacStatu.Repositories
{
    public static class PageMetadataRepo
    {
        public static async Task<bool> ExistsAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return false;
            return await ExistsAsync(id);
        }
        public static async Task<bool> ExistsAsync(string id)
        {

            var es = await Manager.GetESClient_PageMetadataAsync();
            var res = await es.DocumentExistsAsync<PageMetadata>(id);
            return res.Exists;

        }

        public static async Task SaveAsync(PageMetadata item)
        {
            try
            {
                var client = await Repositories.ES.Manager.GetESClient_PageMetadataAsync();
                await client.IndexAsync<PageMetadata>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("PageMetadataRepo.Save error ", e);
                throw;
            }


        }

        public static async Task<bool> DeleteAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return false;
            return await DeleteAsync(id);
        }
        public static async Task<bool> DeleteAsync(string Id)
        {
            var client = await Manager.GetESClient_PageMetadataAsync();
            var res = await client.DeleteAsync<PageMetadata>(Id);
            return res.IsValid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="smlouvaId"></param>
        /// <returns>UniqueId priloh, ktere je nutne zpracovat</returns>
        public static async Task<IEnumerable<Smlouva.Priloha>> MissingInPageMetadata(string smlouvaId)
        {
            var sml = await SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: false);
            if (sml.Prilohy == null)
                return Array.Empty<Smlouva.Priloha>();
            if (sml.Prilohy.Count() == 0)
                return Array.Empty<Smlouva.Priloha>();


            var cl = await HlidacStatu.Repositories.ES.Manager.GetESClient_PageMetadataAsync();
            var res = await cl.SearchAsync<PageMetadata>(s => s
                .Query(q => q
                    .Match(m=>m
                        .Field(f=>f.SmlouvaId)
                        .Query(smlouvaId)
                    )
                )
                .Aggregations(a => a
                        .Terms("perPrilohaId", m => m
                            .Field(f=>f.PrilohaId)
                            .Size(9999)
                        )
                )
                .Size(0)
            );


            var foundPrilohy = ((BucketAggregate)res.Aggregations["perPrilohaId"]).Items
                            .Select(m => ((KeyedBucket<object>)m))
                            .Select(m => m.Key.ToString());

            return sml.Prilohy.Where(m => foundPrilohy.Contains(m.UniqueHash()) == false);
        }

        public static async Task<IEnumerable<PageMetadata>> GetDataForDocument(Smlouva smlouva, Smlouva.Priloha priloha)
        {
            return await GetDataForDocument(smlouva.Id, priloha.UniqueHash());
        }
        public static async Task<IEnumerable<PageMetadata>> GetDataForDocument(string smlouvaId, string prilohaUniqueId)
        {
            var cl = await HlidacStatu.Repositories.ES.Manager.GetESClient_PageMetadataAsync();
            var recs = await Searching.Tools.GetAllRecords<PageMetadata>(cl, 5, $"smlouvaId:{smlouvaId} AND prilohaId:{prilohaUniqueId}");

            return recs;
        }

        public static async Task<PageMetadata> LoadAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return null;
            return await LoadAsync(id);
        }

        public static async Task<PageMetadata> LoadAsync(string id)
        {
            var cl = await ES.Manager.GetESClient_PageMetadataAsync();

            var res = await cl.GetAsync<PageMetadata>(id);
            if (res.Found == false)
                return null;
            else if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
            else
            {
                return res.Source;
            }
        }


    }
}