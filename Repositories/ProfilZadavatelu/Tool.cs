using Devmasters.Batch;

using HlidacStatu.Entities.VZ;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.ProfilZadavatelu
{
    public static class Tool
    {
        public static void ProcessProfilyZadavatelu(bool onlyWithErr, DateTime from, Action<string> outputWriter = null, Action<ActionProgressData> progressWriter = null)
        {
            var profily2 = new List<ProfilZadavatele>();

            Parser parser = Parser.Instance();

            if (onlyWithErr == false) //vsechny profily
            {
                Console.WriteLine("Reading profily zadavatelu");
                Repositories.Searching.Tools.DoActionForAll<ProfilZadavatele>(
                    (pz, obj) =>
                    {
                        profily2.Add(pz.Source);

                        return new ActionOutputData();
                    }, null,
                    outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter,
                    false, elasticClient: Repositories.ES.Manager.GetESClient_VZ(), prefix: "profil zadavatelu ");

                Console.WriteLine("Let's go mining");



            }
            else //jen ty s http chybami
            {
                Console.WriteLine("Reading profily zadavatelu with HTTP error");
                Func<int, int, Nest.ISearchResponse<Entities.Logs.ProfilZadavateleDownload>> searchFunc = (size, page) =>
                {
                    return Repositories.ES.Manager.GetESClient_Logs()
                            .Search<Entities.Logs.ProfilZadavateleDownload>(a => a
                                .Size(size)
                                .Source(ss => ss.Excludes(f => f.Fields("xmlError", "xmlInvalidContent", "httpError")))
                                .From(page * size)
                                .Query(q => q.Term(t => t.Field(f => f.HttpValid).Value(false)))
                                .TrackTotalHits(page * size == 0 ? true : (bool?)null)
                                .Scroll("2m")
                                );
                };
                Repositories.Searching.Tools.DoActionForQuery<Entities.Logs.ProfilZadavateleDownload>(Repositories.ES.Manager.GetESClient_Logs(), searchFunc,
                    (pzd, obj) =>
                    {
                        var profileId = pzd.Source.ProfileId;
                        if (!profily2.Any(m => m.Id == profileId))
                        {
                            var pz = ProfilZadavateleRepo.GetByIdAsync(profileId);
                            if (pz != null)
                                profily2.Add(pz);
                        }
                        return new ActionOutputData();
                    }, null,
                    outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter
                    , false, prefix: "profil zadav 2 ");


            }

            Console.WriteLine("Let's go mining");
            Util.Consts.Logger.Debug("ProfilyZadavatelu: Let's go mining num." + profily2.Count);
            Manager.DoActionForAll<ProfilZadavatele>(Devmasters.Collections.Algorithms.RandomShuffle(profily2),
                (p) =>
                {
                    parser.ProcessProfileZadavateluAsync(p, from);
                    return new ActionOutputData();
                }, outputWriter ?? Manager.DefaultOutputWriter, progressWriter ?? Manager.DefaultProgressWriter, true, prefix: "profil zadav 3 ");


        }

    }



}
