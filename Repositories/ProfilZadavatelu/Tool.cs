using Devmasters.Batch;

using HlidacStatu.Entities.VZ;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Logs;
using Nest;
using Serilog;

namespace HlidacStatu.Repositories.ProfilZadavatelu
{
    public static class Tool
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(Tool));
        
        public static async Task ProcessProfilyZadavateluAsync(bool onlyWithErr, DateTime from, Action<string> outputWriter = null, Action<ActionProgressData> progressWriter = null)
        {
            var profily2 = new List<ProfilZadavatele>();

            Parser parser = Parser.Instance();

            if (onlyWithErr == false) //vsechny profily
            {
                Console.WriteLine("Reading profily zadavatelu");
                await Searching.Tools.DoActionForAllAsync<ProfilZadavatele>(
                    (pz, obj) =>
                    {
                        profily2.Add(pz.Source);

                        return Task.FromResult(new ActionOutputData());
                    }, null,
                    outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter,
                    false, elasticClient: Connectors.Manager.GetESClient_VerejneZakazky(), prefix: "profil zadavatelu ");

                Console.WriteLine("Let's go mining");



            }
            else //jen ty s http chybami
            {
                Console.WriteLine("Reading profily zadavatelu with HTTP error");
                Func<int, int, Task<ISearchResponse<ProfilZadavateleDownload>>> searchFunc = async (size, page) =>
                {
                    var client = Connectors.Manager.GetESClient_Logs(); 
                    return await client.SearchAsync<ProfilZadavateleDownload>(a => a
                                .Size(size)
                                .Source(ss => ss.Excludes(f => f.Fields("xmlError", "xmlInvalidContent", "httpError")))
                                .From(page * size)
                                .Query(q => q.Term(t => t.Field(f => f.HttpValid).Value(false)))
                                .TrackTotalHits(page * size == 0 ? true : (bool?)null)
                                .Scroll("2m")
                                );
                };
                
                await Searching.Tools.DoActionForQueryAsync<ProfilZadavateleDownload>(Connectors.Manager.GetESClient_Logs(), searchFunc, async (pzd, obj) =>
                    {
                        var profileId = pzd.Source.ProfileId;
                        if (!profily2.Any(m => m.Id == profileId))
                        {
                            var pz = await ProfilZadavateleRepo.GetByIdAsync(profileId);
                            if (pz != null)
                                profily2.Add(pz);
                        }
                        return new ActionOutputData();
                    }, null,
                    outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter
                    , false, prefix: "profil zadav 2 ", monitor: new MonitoredTaskRepo.ForBatch());


            }

            Console.WriteLine("Let's go mining");
            _logger.Debug("ProfilyZadavatelu: Let's go mining num." + profily2.Count);
            await Manager.DoActionForAllAsync<ProfilZadavatele>(Devmasters.Collections.Algorithms.RandomShuffle(profily2), async (p) =>
                {
                    await parser.ProcessProfileZadavateluAsync(p, from);
                    return new ActionOutputData();
                }, outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter, true, 
                prefix: "profil zadav 3 ", monitor: new MonitoredTaskRepo.ForBatch());


        }

    }



}
