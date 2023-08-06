using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using System.Linq;
using System.Threading.Tasks;



namespace HlidacStatu.Lib.Data.External.Tables
{
    public class TablesInDocs
    {
        public class Minion
        {
            public static async Task<bool> CreateNewTaskAsync(string smlouvaId, bool force)
            {
                var sml = await SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: false);
                if (sml != null)
                {
                    return await CreateNewTaskAsync(sml, force);
                }
                return false;
            }

            public static async Task<bool> CreateNewTaskAsync(Smlouva sml, bool force)
            {
                var ret = false;
                foreach (var pri in sml.Prilohy)
                {
                    var tbls = HlidacStatu.Lib.Data.External.Tables.SmlouvaPrilohaExtension.GetTablesFromPriloha(sml, pri);


                    if (force
                        || (await DocTablesRepo.ExistsAsync(sml.Id, pri.UniqueHash()))
                        )
                    {
                        var request = new HlidacStatu.DS.Api.TablesInDoc.Task()
                        {
                            smlouvaId = sml.Id,
                            prilohaId = pri.UniqueHash(),
                            url = pri.LocalCopyUrl(sml.Id, true)
                        };

                        using HlidacStatu.Q.Simple.Queue<DS.Api.TablesInDoc.Task> q = new HlidacStatu.Q.Simple.Queue<DS.Api.TablesInDoc.Task>(
                            DS.Api.TablesInDoc.TablesInDocProcessingQueueName,
                            Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                            );
                        q.Send(request);
                        ret = ret || true;
                    }
                }
                return ret;
            }

            public static async Task SaveFinishedTaskAsync(DS.Api.TablesInDoc.ApiResult2 res)
            {
                if (res.results?.Count() > 0)
                {
                    HlidacStatu.DS.Api.TablesInDoc.Result[] tables = res.results;
                    await DocTablesRepo.SaveAsync(res.task.smlouvaId, res.task.prilohaId, tables);
                    
                }
            }

        }
    }
}
