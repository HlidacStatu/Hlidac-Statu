using HlidacStatu.Repositories.ES;
using HlidacStatu.Entities;
using System;
using Nest;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class DocTablesRepo
    {
        private static ElasticClient _doTablesClient = Manager.GetESClient_DocTablesAsync()
    .ConfigureAwait(false).GetAwaiter().GetResult();
        static Devmasters.Cache.AWS_S3.CacheProvider<DocTables.Result[]> awsClient = null;

        static DocTablesRepo()
        {
            awsClient = new Devmasters.Cache.AWS_S3.CacheProvider<DocTables.Result[]>(
                new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero
            );

            awsClient.Init();
        }

        public static async Task<DocTables> GetAsync(string smlouvaId, string prilohaId)
        {
            if (smlouvaId == null || prilohaId == null)
                return null;

            var response = await _doTablesClient.GetAsync<DocTables>(DocTables.GetId(smlouvaId,prilohaId));

            return response.IsValid
                ? response.Source
                : null;
        }

        public static async Task<bool> ExistsAsync(string smlouvaId, string prilohaId)
        {
            if (smlouvaId == null || prilohaId == null)
                throw new ArgumentNullException();

            var response = await _doTablesClient.DocumentExistsAsync<DocTables>(DocTables.GetId(smlouvaId, prilohaId));

            return response.IsValid
                ? response.Exists
                : false;
        }

        public static async Task SaveAsync(string smlouvaId, string prilohaId, DocTables.Result[] tables)
        {
            var dt = new DocTables()
            {
                PrilohaId = prilohaId,
                SmlouvaId = smlouvaId,
                Tables = tables,
                Updated = DateTime.Now
            };
            await SaveAsync(dt);
        }
        public static async Task SaveAsync(DocTables data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));


            var res = await _doTablesClient.IndexAsync(data, o => o.Id(data.Id)); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

        public static async Task ConvertFromMinioAsync(string smlouvaId, string prilohaId, bool rewrite )
        {
            if (rewrite == false)
                if (await ExistsAsync(smlouvaId, prilohaId))
                    return;

            string key = $"SmlouvyPrilohyTbls/{smlouvaId}/{prilohaId}";
            DocTables.Result[] data = awsClient.Get(key);
            if (data == null)
                return;

            await SaveAsync(smlouvaId, prilohaId, data);
        }
        public static async Task DeleteFromMinioAsync(string smlouvaId, string prilohaId)
        {

            string key = $"SmlouvyPrilohyTbls/{smlouvaId}/{prilohaId}";
            DocTables.Result[] data = awsClient.Get(key);
            if (data == null)
                return;

            awsClient.Remove(key);
        }
    }
}