using Devmasters.Batch;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Issues;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories.Searching;

using Nest;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Manager = HlidacStatu.Repositories.ES.Manager;


namespace HlidacStatu.Repositories
{
    public static partial class SmlouvaRepo
    {
        public static Task<bool> DeleteAsync(Smlouva smlouva, ElasticClient client = null)
        {
            return DeleteAsync(smlouva.Id);
        }

        public static async Task<Smlouva[]> GetPodobneSmlouvyAsync(string idSmlouvy, IEnumerable<QueryContainer> mandatory,
            IEnumerable<QueryContainer> optional = null, IEnumerable<string> exceptIds = null, int numOfResults = 50)
        {
            optional = optional ?? new QueryContainer[] { };
            exceptIds = exceptIds ?? new string[] { };
            Smlouva[] _result = null;

            int tryNum = optional.Count();
            while (tryNum >= 0)
            {
                var query = mandatory.Concat(optional.Take(tryNum)).ToArray();
                tryNum--;

                var tmpResult = new List<Smlouva>();
                var res = await SmlouvaRepo.Searching.RawSearchAsync(
                    new QueryContainerDescriptor<Smlouva>().Bool(b => b.Must(query)),
                    1, numOfResults, SmlouvaRepo.Searching.OrderResult.DateAddedDesc, null
                );
                var resN = await SmlouvaRepo.Searching.RawSearchAsync(
                    new QueryContainerDescriptor<Smlouva>().Bool(b => b.Must(query)),
                    1, numOfResults, SmlouvaRepo.Searching.OrderResult.DateAddedDesc, null, platnyZaznam: false
                );

                if (res.IsValid == false)
                    Manager.LogQueryError<Smlouva>(res);
                else
                    tmpResult.AddRange(res.Hits.Select(m => m.Source).Where(m => m.Id != idSmlouvy));
                if (resN.IsValid == false)
                    Manager.LogQueryError<Smlouva>(resN);
                else
                    tmpResult.AddRange(resN.Hits.Select(m => m.Source).Where(m => m.Id != idSmlouvy));

                if (tmpResult.Count > 0)
                {
                    var resSml = tmpResult.Where(m =>
                        m.Id != idSmlouvy
                        && !exceptIds.Any(id => id == m.Id)
                    ).ToArray();
                    if (resSml.Length > 0)
                        _result = resSml;
                }
            }

            _result ??= new Smlouva[] { };

            return _result.Take(numOfResults).ToArray();
        }

        private static void PrepareBeforeSave(Smlouva smlouva, bool updateLastUpdateValue = true)
        {
            smlouva.SVazbouNaPolitiky = smlouva.JeSmlouva_S_VazbouNaPolitiky(Relation.AktualnostType.Libovolny);
            smlouva.SVazbouNaPolitikyNedavne = smlouva.JeSmlouva_S_VazbouNaPolitiky(Relation.AktualnostType.Nedavny);
            smlouva.SVazbouNaPolitikyAktualni = smlouva.JeSmlouva_S_VazbouNaPolitiky(Relation.AktualnostType.Aktualni);

            if (updateLastUpdateValue)
                smlouva.LastUpdate = DateTime.Now;

            smlouva.ConfidenceValue = smlouva.GetConfidenceValue();

            /////// HINTS

            if (smlouva.Hint == null)
                smlouva.Hint = new HintSmlouva();


            smlouva.Hint.Updated = DateTime.Now;

            smlouva.Hint.SkrytaCena = smlouva.Issues?
                .Any(m => m.IssueTypeId == (int)IssueType.IssueTypes.Nulova_hodnota_smlouvy) == true
                ? 1
                : 0;

            Firma fPlatce = Firmy.Get(smlouva.Platce.ico);
            Firma[] fPrijemci = smlouva.Prijemce.Select(m => m.ico)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => Firmy.Get(m))
                .Where(f => f.Valid)
                .ToArray();

            List<Firma> firmy = fPrijemci
                .Concat(new Firma[] { fPlatce })
                .Where(f => f.Valid)
                .ToList();

            smlouva.Hint.DenUzavreni = (int)Devmasters.DT.Util.TypeOfDay(smlouva.datumUzavreni);

            if (!firmy.Any())
                smlouva.Hint.PocetDniOdZalozeniFirmy = 9999;
            else
                smlouva.Hint.PocetDniOdZalozeniFirmy = (int)firmy
                    .Select(f => (smlouva.datumUzavreni - (f.Datum_Zapisu_OR ?? new DateTime(1990, 1, 1))).TotalDays)
                    .Min();

            smlouva.Hint.SmlouvaSPolitickyAngazovanymSubjektem = (int)HintSmlouva.PolitickaAngazovanostTyp.Neni;
            if (firmy.Any(f => f.IsSponzorBefore(smlouva.datumUzavreni)))
                smlouva.Hint.SmlouvaSPolitickyAngazovanymSubjektem =
                    (int)HintSmlouva.PolitickaAngazovanostTyp.PrimoSubjekt;
            else if (firmy.Any(f => f.MaVazbyNaPolitikyPred(smlouva.datumUzavreni)))
                smlouva.Hint.SmlouvaSPolitickyAngazovanymSubjektem =
                    (int)HintSmlouva.PolitickaAngazovanostTyp.AngazovanyMajitel;

            if (fPlatce.Valid && fPlatce.PatrimStatu())
            {
                if (fPrijemci.All(f => f.PatrimStatu()))
                    smlouva.Hint.VztahSeSoukromymSubjektem =
                        (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.PouzeStatStat;
                else if (fPrijemci.All(f => f.PatrimStatu() == false))
                    smlouva.Hint.VztahSeSoukromymSubjektem =
                        (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.PouzeStatSoukr;
                else
                    smlouva.Hint.VztahSeSoukromymSubjektem = (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.Kombinovane;
            }

            if (fPlatce.Valid && fPlatce.PatrimStatu() == false)
            {
                if (fPrijemci.All(f => f.PatrimStatu()))
                    smlouva.Hint.VztahSeSoukromymSubjektem =
                        (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.PouzeStatSoukr;
                else if (fPrijemci.All(f => f.PatrimStatu() == false))
                    smlouva.Hint.VztahSeSoukromymSubjektem =
                        (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.PouzeSoukrSoukr;
                else
                    smlouva.Hint.VztahSeSoukromymSubjektem = (int)HintSmlouva.VztahSeSoukromymSubjektemTyp.Kombinovane;
            }

            //U limitu
            smlouva.Hint.SmlouvaULimitu = (int)HintSmlouva.ULimituTyp.OK;
            //vyjimky
            //smlouvy s bankama o repo a vkladech
            bool vyjimkaNaLimit = false;
            if (vyjimkaNaLimit == false)
            {
                if (
                    (
                        smlouva.hodnotaBezDph >= Consts.Limit1bezDPH_From
                        && smlouva.hodnotaBezDph <= Consts.Limit1bezDPH_To
                    )
                    ||
                    (
                        smlouva.CalculatedPriceWithVATinCZK > Consts.Limit1bezDPH_From * 1.21m
                        && smlouva.CalculatedPriceWithVATinCZK <= Consts.Limit1bezDPH_To * 1.21m
                    )
                )
                    smlouva.Hint.SmlouvaULimitu = (int)HintSmlouva.ULimituTyp.Limit2M;

                if (
                    (
                        smlouva.hodnotaBezDph >= Consts.Limit2bezDPH_From
                        && smlouva.hodnotaBezDph <= Consts.Limit2bezDPH_To
                    )
                    ||
                    (
                        smlouva.CalculatedPriceWithVATinCZK > Consts.Limit2bezDPH_From * 1.21m
                        && smlouva.CalculatedPriceWithVATinCZK <= Consts.Limit2bezDPH_To * 1.21m
                    )
                )
                    smlouva.Hint.SmlouvaULimitu = (int)HintSmlouva.ULimituTyp.Limit6M;
            }

            if (smlouva.Prilohy != null)
            {
                foreach (var p in smlouva.Prilohy)
                    p.UpdateStatistics(smlouva);
            }

            if (ClassificationOverrideRepo.TryGetOverridenClassification(smlouva.Id, out var classificationOverride))
            {
                var types = new List<Smlouva.SClassification.Classification>();
                if (classificationOverride.CorrectCat1.HasValue)
                    types.Add(new Smlouva.SClassification.Classification()
                    {
                        TypeValue = classificationOverride.CorrectCat1.Value,
                        ClassifProbability = 0.9m
                    });
                if (classificationOverride.CorrectCat2.HasValue)
                    types.Add(new Smlouva.SClassification.Classification()
                    {
                        TypeValue = classificationOverride.CorrectCat2.Value,
                        ClassifProbability = 0.8m
                    });

                smlouva.Classification = new Smlouva.SClassification(types.ToArray());
            }
        }



        public static bool JeSmlouva_S_VazbouNaPolitiky(this Smlouva smlouva, Relation.AktualnostType aktualnost)
        {
            var icos = ico_s_VazbouPolitik;
            if (aktualnost == Relation.AktualnostType.Nedavny)
                icos = ico_s_VazbouPolitikNedavne;
            if (aktualnost == Relation.AktualnostType.Aktualni)
                icos = ico_s_VazbouPolitikAktualni;

            Firma f = null;
            if (smlouva.platnyZaznam)
            {
                f = Firmy.Get(smlouva.Platce.ico);

                if (f.Valid && !f.PatrimStatu())
                {
                    if (!string.IsNullOrEmpty(smlouva.Platce.ico) && icos.Contains(smlouva.Platce.ico))
                        return true;
                }

                foreach (var ss in smlouva.Prijemce)
                {
                    f = Firmy.Get(ss.ico);
                    if (f.Valid && !f.PatrimStatu())
                    {
                        if (!string.IsNullOrEmpty(ss.ico) && icos.Contains(ss.ico))
                            return true;
                    }
                }
            }

            return false;
        }

        static HashSet<string> ico_s_VazbouPolitik = new HashSet<string>(
            StaticData.FirmySVazbamiNaPolitiky_vsechny_Cache.Get().SoukromeFirmy.Select(m => m.Key)
                .Union(StaticData.SponzorujiciFirmy_Vsechny.Get().Select(m => m.IcoDarce))
                .Distinct()
        );

        static HashSet<string> ico_s_VazbouPolitikAktualni = new HashSet<string>(
            StaticData.FirmySVazbamiNaPolitiky_aktualni_Cache.Get().SoukromeFirmy.Select(m => m.Key)
                .Union(StaticData.SponzorujiciFirmy_Nedavne.Get().Select(m => m.IcoDarce))
                .Distinct()
        );

        static HashSet<string> ico_s_VazbouPolitikNedavne = new HashSet<string>(
            StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy.Select(m => m.Key)
                .Union(StaticData.SponzorujiciFirmy_Nedavne.Get().Select(m => m.IcoDarce))
                .Distinct()
        );

        public static string SmlouvaProcessingQueueName = "smlouvy2Process";
        public static bool AddToProcessingQueue(this Smlouva smlouva,
                bool forceOCR = false,
                bool forceClassification = false,
                bool forceTablesMining = false,
                bool forceBlurredPages = false
            )
        {
            return AddToProcessingQueue(smlouva.Id, forceOCR, forceClassification, forceTablesMining, forceBlurredPages);

        }
        public static bool AddToProcessingQueue(this string smlouvaId,
        bool forceOCR = false,
        bool forceClassification = false,
        bool forceTablesMining = false,
        bool forceBlurredPages = false
    )
        {

            using HlidacStatu.Q.Simple.Queue<Smlouva.Queued> q = new Q.Simple.Queue<Smlouva.Queued>(
                SmlouvaProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            var sq = new Smlouva.Queued()
            {
                SmlouvaId = smlouvaId,                 
                ForceBlurredPages = forceBlurredPages,
                ForceClassification = forceClassification,
                ForceOCR = forceOCR,
                ForceTablesMining = forceTablesMining
            };
            q.Send(sq);

            return true;
        }
        public static Smlouva.Queued GetSmlouvaFromProcessingQueue()
        {
            using HlidacStatu.Q.Simple.Queue<Smlouva.Queued> q = new Q.Simple.Queue<Smlouva.Queued>(
                SmlouvaProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            ulong? id = null;
            var sq = q.GetAndAck(out id);
            if (sq != null)
            {
                sq.ItemIdInQueue = id;
            }
            return sq;
        }

        public static async Task<bool> SaveAsync(Smlouva smlouva, ElasticClient client = null, bool updateLastUpdateValue = true, bool skipPrepareBeforeSave = false)
        {
            if (smlouva == null)
                return false;

            if (skipPrepareBeforeSave == false)
                PrepareBeforeSave(smlouva, updateLastUpdateValue);

            ElasticClient c = client;
            if (c == null)
            {
                if (smlouva.platnyZaznam)
                    c = await Repositories.ES.Manager.GetESClientAsync();
                else
                    c = await Repositories.ES.Manager.GetESClient_SneplatneAsync();
            }

            var res = await c.IndexAsync<Smlouva>(smlouva, m => m.Id(smlouva.Id));

            if (smlouva.platnyZaznam == false && res.IsValid)
            {
                //zkontroluj zda neni v indexu s platnymi. pokud ano, smaz ho tam
                var cExist = await Repositories.ES.Manager.GetESClientAsync();
                var s = await LoadAsync(smlouva.Id, cExist);
                if (s != null)
                    await DeleteAsync(smlouva.Id, cExist);
            }

            if (res.IsValid)
            {
                SaveSmlouvaDataIntoDB(smlouva);
            }

            return res.IsValid;
        }

        public static void SaveSmlouvaDataIntoDB(Smlouva smlouva)
        {
            try
            {
                DirectDB.NoResult("exec smlouvaId_save @id,@active, @created, @updated, @icoOdberatele",
                    new SqlParameter("id", smlouva.Id),
                    new SqlParameter("created", smlouva.casZverejneni),
                    new SqlParameter("updated", smlouva.LastUpdate),
                    new SqlParameter("active", smlouva.znepristupnenaSmlouva() ? (int)0 : (int)1),
                    new SqlParameter("icoOdberatele", smlouva.Platce.ico)
                );

                DirectDB.NoResult("delete from SmlouvyDodavatele where smlouvaId= @smlouvaId",
                    new SqlParameter("smlouvaId", smlouva.Id)
                );
                if (smlouva.Prijemce != null)
                {
                    foreach (var ico in smlouva.Prijemce.Select(m => m.ico).Distinct())
                    {
                        if (!string.IsNullOrEmpty(ico))
                            DirectDB.NoResult("insert into SmlouvyDodavatele(smlouvaid,ico) values(@smlouvaid, @ico)",
                                new SqlParameter("smlouvaId", smlouva.Id),
                                new SqlParameter("ico", ico)
                        );

                    }
                }
            }
            catch (Exception e)
            {
                Manager.ESLogger.Error("Manager Save", e);
            }


            if (!string.IsNullOrEmpty(smlouva.Platce?.ico))
            {
                DirectDB.NoResult("exec Firma_IsInRS_Save @ico",
                    new SqlParameter("ico", smlouva.Platce?.ico)
                );
            }

            if (!string.IsNullOrEmpty(smlouva.VkladatelDoRejstriku?.ico))
            {
                DirectDB.NoResult("exec Firma_IsInRS_Save @ico",
                    new SqlParameter("ico", smlouva.VkladatelDoRejstriku?.ico)
                );
            }

            foreach (var s in smlouva.Prijemce ?? new Smlouva.Subjekt[] { })
            {
                if (!string.IsNullOrEmpty(s.ico))
                {
                    DirectDB.NoResult("exec Firma_IsInRS_Save @ico",
                        new SqlParameter("ico", s.ico)
                    );
                }
            }
        }



        public static async Task ZmenStavSmlouvyNaAsync(Smlouva smlouva, bool platnyZaznam)
        {
            var issueTypeId = -1;
            var issue = new Issue(null, issueTypeId, "Smlouva byla znepřístupněna",
                "Na žádost subjektu byla tato smlouva znepřístupněna.", ImportanceLevel.Formal,
                permanent: true);

            if (platnyZaznam && smlouva.znepristupnenaSmlouva())
            {
                smlouva.platnyZaznam = platnyZaznam;
                //zmen na platnou
                if (smlouva.Issues.Any(m => m.IssueTypeId == issueTypeId))
                {
                    smlouva.Issues = smlouva.Issues
                        .Where(m => m.IssueTypeId != issueTypeId)
                        .ToArray();
                }

                await SaveAsync(smlouva);
            }
            else if (platnyZaznam == false && smlouva.znepristupnenaSmlouva() == false)
            {
                smlouva.platnyZaznam = platnyZaznam;
                if (!smlouva.Issues.Any(m => m.IssueTypeId == -1))
                    smlouva.AddSpecificIssue(issue);
                await SaveAsync(smlouva);
            }
        }

        public static async Task<IEnumerable<string>> _allIdsFromESAsync(bool deleted, Action<string> outputWriter = null,
            Action<ActionProgressData> progressWriter = null)
        {
            List<string> ids = new List<string>();
            var client = deleted ? await Repositories.ES.Manager.GetESClient_SneplatneAsync() : await Repositories.ES.Manager.GetESClientAsync();

            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc =
                searchFunc = async (size, page) =>
                {
                    return await client.SearchAsync<Smlouva>(a => a
                        .Size(size)
                        .From(page * size)
                        .Source(false)
                        .Query(q => q.Term(t => t.Field(f => f.platnyZaznam).Value(deleted ? false : true)))
                        .Scroll("1m")
                    );
                };


            await Tools.DoActionForQueryAsync<Smlouva>(client,
                searchFunc, (hit, param) =>
                {
                    ids.Add(hit.Id);
                    return new ActionOutputData() { CancelRunning = false, Log = null };
                }, null, outputWriter, progressWriter, false, blockSize: 100);

            return ids;
        }

        public static IEnumerable<string> AllIdsFromDB()
        {
            return AllIdsFromDB(null);
        }

        public static IEnumerable<string> AllIdsFromDB(bool? deleted, DateTime? from = null)
        {
            List<string> ids = null;
            using (DbEntities db = new DbEntities())
            {
                IQueryable<SmlouvyId> q = db.SmlouvyIds;
                if (deleted.HasValue)
                    q = q.Where(m => m.Active == (deleted.Value ? 0 : 1));
                if (from.HasValue)
                    q = q.Where(m => m.Updated > from);

                ids = q.Select(m => m.Id)
                    .ToList();
            }

            return ids;
        }

        public static Task<IEnumerable<string>> AllIdsFromESAsync()
        {
            return AllIdsFromESAsync(null);
        }

        public static async Task<IEnumerable<string>> AllIdsFromESAsync(bool? deleted, Action<string> outputWriter = null,
            Action<ActionProgressData> progressWriter = null)
        {
            if (deleted.HasValue)
                return await _allIdsFromESAsync(deleted.Value, outputWriter, progressWriter);
            else
                return
                    (await _allIdsFromESAsync(false, outputWriter, progressWriter))
                        .Union(await _allIdsFromESAsync(true, outputWriter, progressWriter));
        }

        public static async Task<bool> DeleteAsync(string Id, ElasticClient client = null)
        {
            client ??= await Repositories.ES.Manager.GetESClientAsync();
            var res = await client.DeleteAsync<Smlouva>(Id);
            return res.IsValid;
        }

        public static async Task<bool> ExistsZaznamAsync(string id, ElasticClient client = null)
        {
            bool noSetClient = client == null;
            client ??= await Repositories.ES.Manager.GetESClientAsync();
            var res = await client.DocumentExistsAsync<Smlouva>(id);
            if (noSetClient)
            {
                if (res.Exists)
                    return true;
                client = await Repositories.ES.Manager.GetESClient_SneplatneAsync();
                res = await client.DocumentExistsAsync<Smlouva>(id);
                return res.Exists;
            }
            else
                return res.Exists;
        }


        public static async Task<Smlouva> LoadAsync(string idVerze, ElasticClient client = null, bool includePrilohy = true)
        {
            var s = await _loadAsync(idVerze, client, includePrilohy);
            if (s == null)
                return s;
            _ = s.GetRelevantClassification();
            if (s.Classification?.Version == 1 && s.Classification?.Types != null)
            {
                s.Classification.ConvertToV2();
                await SaveAsync(s, null, false);
            }

            return s;
        }

        private static async Task<Smlouva> _loadAsync(string idVerze, ElasticClient client = null, bool includePrilohy = true)
        {
            bool specClient = client != null;
            try
            {
                ElasticClient c = null;
                if (specClient)
                    c = client;
                else
                    c = await Repositories.ES.Manager.GetESClientAsync();

                //var res = c.Get<Smlouva>(idVerze);

                var res = includePrilohy
                    ? await c.GetAsync<Smlouva>(idVerze)
                    : await c.GetAsync<Smlouva>(idVerze, s => s.SourceExcludes("prilohy.plainTextContent"));


                if (res.Found)
                    return res.Source;
                else
                {
                    if (specClient == false)
                    {
                        var c1 = await Repositories.ES.Manager.GetESClient_SneplatneAsync();

                        res = includePrilohy
                            ? await c1.GetAsync<Smlouva>(idVerze)
                            : await c1.GetAsync<Smlouva>(idVerze, s => s.SourceExcludes(sml => sml.Prilohy));

                        if (res.Found)
                            return res.Source;
                        else if (res.IsValid)
                        {
                            Manager.ESLogger.Warning("Valid Req: Cannot load Smlouva Id " + idVerze +
                                                     "\nDebug:" + res.DebugInformation);
                            //DirectDB.NoResult("delete from SmlouvyIds where id = @id", new System.Data.SqlClient.SqlParameter("id", idVerze));
                        }
                        else if (res.Found == false)
                            return null;
                        else if (res.ServerError.Status == 404)
                            return null;
                        else
                        {
                            Manager.ESLogger.Error(
                                "Invalid Req: Cannot load Smlouva Id " + idVerze + "\n Debug:" + res.DebugInformation +
                                " \nServerError:" + res.ServerError?.ToString(), res.OriginalException);
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Manager.ESLogger.Error("Cannot load Smlouva Id " + idVerze, e);
                return null;
            }
        }

    }
}