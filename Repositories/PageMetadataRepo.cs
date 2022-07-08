using System;
using System.Threading.Tasks;

using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using Microsoft.EntityFrameworkCore;

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
            var res= await es.DocumentExistsAsync<PageMetadata>(id);
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