using Devmasters.Batch;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Api;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Issues;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Searching;
using Nest;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.KIndex;
using Manager = HlidacStatu.Connectors.Manager;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using HlidacStatu.MLUtil.Splitter;
using System.Text;
using static HlidacStatu.AI.LLM.ContractParties.Parsed;


namespace HlidacStatu.Repositories
{
    public static partial class SmlouvaRepo
    {


        internal static AI.LLM.ContractParties.Parsed ToContractParties(this Smlouva smlouva)
        {
            AI.LLM.ContractParties.Parsed res = new AI.LLM.ContractParties.Parsed();
            res.objednatel = new AI.LLM.ContractParties.Parsed.Subjekt()
            {
                IC = smlouva.Platce.ico,
            };
            res.poskytovatele = smlouva.Prijemce.Select(m =>
                new AI.LLM.ContractParties.Parsed.Subjekt()
                {
                    IC = m.ico
                })
                .ToList();

            return res;
        }

        public async static Task<bool> SameContractPartiesWithAIAsync(Smlouva smlouva,
            HlidacStatu.AI.LLM.Clients.BaseClient llmClient, int maxWordsFromBeginningOfTheText = 1000,
            HlidacStatu.AI.LLM.Models.Model model = null)
        {
            if (smlouva == null)
                throw new ArgumentNullException(nameof(smlouva));

            foreach (var p in smlouva.Prilohy)
            {
                if (!string.IsNullOrEmpty(p?.PlainTextContent))
                {
                    HlidacStatu.AI.LLM.ContractParties.Comparison compareRes = await ContractPartiesComparisonWithAIAsync(
                        smlouva, p, llmClient, maxWordsFromBeginningOfTheText, model);
                    if (compareRes.Same)
                        return true;
                }

            }

            return false;
        }


        public static string GetBeginningOfTheContract(Smlouva smlouva, Smlouva.Priloha priloha,
            int maxWordsFromBeginningOfTheText = 1000)
        {

            if (smlouva == null)
                throw new ArgumentNullException(nameof(smlouva));
            if (priloha == null)
                throw new ArgumentNullException(nameof(priloha));


            StringBuilder t = new StringBuilder();
            SplitSmlouva smlSplit = SplitSmlouva.Create(smlouva.Id, priloha.UniqueHash(), priloha.PlainTextContent);

            for (int i = 0; i < smlSplit.Sections.Count; i++)
            {
                var sect = smlSplit.Sections[i];
                t.AppendLine(sect.ToText());
                if (i >= 1 || HlidacStatu.AI.LLM.Util.TokenCount(t.ToString(), HlidacStatu.AI.LLM.Util.TokenizeAlgorithm.simple) > maxWordsFromBeginningOfTheText)
                    break;
            }

            return t.ToString();
        }
        public async static Task<HlidacStatu.AI.LLM.ContractParties.Comparison> ContractPartiesComparisonWithAIAsync(
                Smlouva smlouva, Smlouva.Priloha priloha,
                    HlidacStatu.AI.LLM.Clients.BaseClient llmClient, int maxWordsFromBeginningOfTheText = 1000,
                    HlidacStatu.AI.LLM.Models.Model model = null, bool doubleCheck = false)
        {

            AI.LLM.ContractParties.Parsed smlouvaParties = ToContractParties(smlouva);


            HlidacStatu.AI.LLM.ContractParties llm = new AI.LLM.ContractParties(llmClient);
            model = model ?? HlidacStatu.AI.LLM.Models.Model.Llama31;

            string t = GetBeginningOfTheContract(smlouva, priloha, maxWordsFromBeginningOfTheText);
            AI.LLM.ContractParties.Parsed contractPartiesRes = await llm.FindContractPartiesAsync(t, maxWordsFromBeginningOfTheText, model);

            var res = new HlidacStatu.AI.LLM.ContractParties.Comparison(smlouvaParties, contractPartiesRes);

            if (res.Same == false && doubleCheck)
            {

                bool objednatelIsTrue = false;
                bool dodavateleAreTrue = true;
                if (res.Status == AI.LLM.ContractParties.Comparison.StatusEnum.SwitchedContractParties
                    ||
                    res.Status == AI.LLM.ContractParties.Comparison.StatusEnum.DifferentPlatce
                    )
                {
                    Firma objednatel = Firmy.Get(res.SecondToCompare.objednatel.GetOnlyIco());
                    if (objednatel.Valid)
                    {
                        objednatelIsTrue = await llm.DoubleCheckParties(objednatel.Jmeno, objednatel.ICO, true, t.ToString());
                    }
                }
                if (res.Status == AI.LLM.ContractParties.Comparison.StatusEnum.SwitchedContractParties
                    ||
                    res.Status == AI.LLM.ContractParties.Comparison.StatusEnum.DifferentPrijemce
                    )
                {
                    foreach (var d in res.SecondToCompare.poskytovatele)
                    {
                        Firma dodavatel = Firmy.Get(res.SecondToCompare.objednatel.GetOnlyIco());
                        if (dodavatel.Valid)
                        {
                            dodavateleAreTrue = dodavateleAreTrue &
                                (
                                    await llm.DoubleCheckParties(dodavatel.Jmeno, dodavatel.ICO, true, t.ToString())
                                );
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Deletes a Smlouva asynchronously.
        /// </summary>
        /// <param name="smlouva">The Smlouva to be deleted.</param>
        /// <param name="client">Optional. The ElasticClient to use for the deletion. If not provided, a default client will be used.</param>
        /// <returns>A Task representing the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
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
            StaticData.FirmySVazbamiNaPolitiky_vsechny_Cache.Get()?
                .SoukromeFirmy?
                .Select(m => m.Key)?
                .Union(
                    StaticData.SponzorujiciFirmy_Vsechny?
                        .Get()?
                        .Select(m => m.IcoDarce)
                        ?? new string[] { }
                )?
                .Distinct()
            ?? new string[] { }
        );

        static HashSet<string> ico_s_VazbouPolitikAktualni = new HashSet<string>(
            StaticData.FirmySVazbamiNaPolitiky_aktualni_Cache.Get()
                .SoukromeFirmy?
                .Select(m => m.Key)?
                .Union(
                    StaticData.SponzorujiciFirmy_Nedavne?
                        .Get()?
                        .Select(m => m.IcoDarce)
                        ?? new string[] { }
                )?
                .Distinct()
            ?? new string[] { }

        );

        static HashSet<string> ico_s_VazbouPolitikNedavne = new HashSet<string>(
            StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                .SoukromeFirmy?
                .Select(m => m.Key)?
                .Union(
                    StaticData.SponzorujiciFirmy_Nedavne?
                        .Get()?
                        .Select(m => m.IcoDarce)
                        ?? new string[] { }
                )?
                .Distinct()
            ?? new string[] { }

        );

        //public static string SmlouvaProcessingQueueName = "smlouvy2Process";
        //public static string SmlouvaProcessingQueueNameOnDemand = "smlouvy2ProcessOnDemand";
        public static bool AddToProcessingQueue(this Smlouva smlouva,
                bool forceOCR = false,
                bool forceClassification = false,
                bool forceTablesMining = false,
                bool forceBlurredPages = false,
            int priority = (int)OcrWork.TaskPriority.Standard
            )
        {
            return AddToProcessingQueue(smlouva.Id, forceOCR, forceClassification, forceTablesMining, forceBlurredPages, priority);
        }
        public static bool AddToProcessingQueue(this string smlouvaId,
        bool forceOCR = false,
        bool forceClassification = false,
        bool forceTablesMining = false,
        bool forceBlurredPages = false,
        int priority = (int)OcrWork.TaskPriority.Standard

    )
        {
            return AddToProcessingQueue(new string[] { smlouvaId },
                forceOCR, forceClassification, forceTablesMining, forceBlurredPages, priority);
        }

        public static bool AddToProcessingQueue(IEnumerable<string> smlouvyIds,
            bool forceOCR = false,
            bool forceClassification = false,
            bool forceTablesMining = false,
            bool forceBlurredPages = false,
            int priority = (int)OcrWork.TaskPriority.Standard
            )
        {
            return AddToProcessingQueue(smlouvyIds, priority,
            new DS.Api.OcrWork.TaskOptions()
            {
                forceOCR = forceOCR,
                forceBlurredPages = forceBlurredPages,
                forceClassification = forceClassification,
                forceTablesMining = forceTablesMining
            });
        }


        public static bool AddToProcessingQueue(string smlouvaId,
            int priority = (int)OcrWork.TaskPriority.Standard,
            DS.Api.OcrWork.TaskOptions options = null
            )
        {
            return AddToProcessingQueue(new string[] { smlouvaId }, priority, options);
        }

        public static bool AddToProcessingQueue(IEnumerable<string> smlouvyIds,
            int priority = (int)OcrWork.TaskPriority.Standard,
            DS.Api.OcrWork.TaskOptions options = null
            )
        {
            var res = true;
            foreach (var id in smlouvyIds)
            {
                ItemToOcrQueue.AddNewTask(DS.Api.OcrWork.DocTypes.Smlouva,
                    id, null,
                    priority,
                    options
            );

            }
            return res;
        }

        /*        public static Smlouva.Queued GetSmlouvaFromProcessingQueue(bool onDemandQueue = false)
                {
                    string queueName = onDemandQueue ? SmlouvaProcessingQueueNameOnDemand : SmlouvaProcessingQueueName;

                    using HlidacStatu.Q.Simple.Queue<Smlouva.Queued> q = new Q.Simple.Queue<Smlouva.Queued>(
                        queueName,
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
        */


        public static DS.Api.OcrWork.Task GetSmlouvaWithFinishedOCR()
        {
            using HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task> q = new HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task>(
                DS.Api.OcrWork.OCRDoneProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            ulong? id = null;
            var sq = q.GetAndAck(out id);
            if (sq != null)
                sq.options = sq.options ?? new OcrWork.TaskOptions();
            return sq;
        }

        public static bool FireOCRDoneEvent(Smlouva smlouva, OcrWork.TaskOptions options = null)
        {
            return FireOCRDoneEvent(smlouva?.Id, options);
        }
        public static bool FireOCRDoneEvent(IEnumerable<string> smlouvyIds, OcrWork.TaskOptions options = null)
        {
            bool ok = true;

            using HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task> q = new HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task>(
                DS.Api.OcrWork.OCRDoneProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );
            var tasks = smlouvyIds.Select(m => new DS.Api.OcrWork.Task()
            {
                parentDocId = m,
                type = DS.Api.OcrWork.DocTypes.Smlouva,
                options = options
            });
            q.Send(tasks);
            return ok;
        }
        public static bool FireOCRDoneEvent(string smlouvaId, OcrWork.TaskOptions options = null)
        {

            if (smlouvaId == null)
                return false;

            using HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task> q = new HlidacStatu.Q.Simple.Queue<DS.Api.OcrWork.Task>(
                DS.Api.OcrWork.OCRDoneProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );
            var task = new DS.Api.OcrWork.Task()
            {
                parentDocId = smlouvaId,
                type = DS.Api.OcrWork.DocTypes.Smlouva,
                options = options
            };
            q.Send(task);

            return true;
        }
        public static async Task<bool> SaveAsync(Smlouva smlouva,
            ElasticClient client = null,
            bool updateLastUpdateValue = true,
            bool skipPrepareBeforeSave = false,
            bool fireOCRDone = false)
        {
            if (smlouva == null)
                return false;


            if (skipPrepareBeforeSave == false)
                PrepareBeforeSave(smlouva, updateLastUpdateValue);

            //update statistics of subjects
            bool updateStat = false;
            var preSml = await LoadAsync(smlouva.Id, includePrilohy: false);
            if (preSml == null) //nova smlouva
                updateStat = true;
            else
            {
                updateStat = updateStat || preSml.Platce.ico != smlouva.Platce.ico;
                updateStat = updateStat || preSml.Prijemce.Length != smlouva.Prijemce.Length;
                updateStat = updateStat || preSml.CalculatedPriceWithVATinCZK != smlouva.CalculatedPriceWithVATinCZK;
                if (updateStat == false)
                {
                    foreach (var pr in preSml.Prijemce)
                    {
                        updateStat = updateStat || smlouva.Prijemce.Any(p => p.ico == pr.ico) == false;
                    }
                    foreach (var pr in smlouva.Prijemce)
                    {
                        updateStat = updateStat || preSml.Prijemce.Any(p => p.ico == pr.ico) == false;
                    }
                }
            }


            ElasticClient c = client;
            if (c == null)
            {
                if (smlouva.platnyZaznam)
                    c = await Manager.GetESClientAsync();
                else
                    c = await Manager.GetESClient_SneplatneAsync();
            }

            var res = await c.IndexAsync<Smlouva>(smlouva, m => m.Id(smlouva.Id));

            if (smlouva.platnyZaznam == false && res.IsValid)
            {
                //zkontroluj zda neni v indexu s platnymi. pokud ano, smaz ho tam
                var cExist = await Manager.GetESClientAsync();
                var s = await LoadAsync(smlouva.Id, cExist);
                if (s != null)
                    _ = await DeleteAsync(smlouva.Id, cExist);
            }

            if (res.IsValid)
            {
                SaveSmlouvaDataIntoDB(smlouva);
            }

            if (updateStat)
            {
                RecalculateItemRepo.AddFirmaToProcessingQueue(smlouva.Platce.ico, RecalculateItem.StatisticsTypeEnum.Smlouva, $"smlouva {smlouva.Id}");
                foreach (var pr in smlouva.Prijemce)
                    RecalculateItemRepo.AddFirmaToProcessingQueue(pr.ico, RecalculateItem.StatisticsTypeEnum.Smlouva, $"smlouva {smlouva.Id}");
            }

            if (fireOCRDone == true)
                FireOCRDoneEvent(smlouva);
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
                _logger.Error(e, "Manager Save");
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

                await SaveAsync(smlouva, fireOCRDone: false
                    );
            }
            else if (platnyZaznam == false && smlouva.znepristupnenaSmlouva() == false)
            {
                smlouva.platnyZaznam = platnyZaznam;
                if (!smlouva.Issues.Any(m => m.IssueTypeId == -1))
                    smlouva.AddSpecificIssue(issue);
                await SaveAsync(smlouva, fireOCRDone: false);
            }
        }

        public static async Task<IEnumerable<string>> _allIdsFromESAsync(bool deleted, Action<string> outputWriter = null,
            Action<ActionProgressData> progressWriter = null)
        {
            List<string> ids = new List<string>();
            var client = deleted ? await Manager.GetESClient_SneplatneAsync() : await Manager.GetESClientAsync();

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
            client ??= await Manager.GetESClientAsync();
            var res = await client.DeleteAsync<Smlouva>(Id);
            return res.IsValid;
        }

        public static async Task<bool> ExistsZaznamAsync(string id, ElasticClient client = null)
        {
            bool noSetClient = client == null;
            client ??= await Manager.GetESClientAsync();
            var res = await client.DocumentExistsAsync<Smlouva>(id);
            if (noSetClient)
            {
                if (res.Exists)
                    return true;
                client = await Manager.GetESClient_SneplatneAsync();
                res = await client.DocumentExistsAsync<Smlouva>(id);
                return res.Exists;
            }
            else
                return res.Exists;
        }

        public static async Task<T> GetPartValueAsync<T>(string idVerze, string elasticPropertyPath, string jsonPath = null,
        ElasticClient client = null)
        //where T : class
        {
            var res = await GetPartValuesAsync<T>(idVerze, elasticPropertyPath, jsonPath, client);
            if (res == null)
                return default(T);
            return res.FirstOrDefault<T>();
        }
        public static async Task<T[]> GetPartValuesAsync<T>(string idVerze, string elasticPropertyPath, string jsonPath = null,
           ElasticClient client = null)
        //where T : class
        {
            jsonPath = jsonPath ?? elasticPropertyPath;
            bool specClient = client != null;
            try
            {
                ElasticClient c = null;
                if (specClient)
                    c = client;
                else
                    c = await HlidacStatu.Connectors.Manager.GetESClientAsync();

                //var res = c.Get<Smlouva>(idVerze);

                var res = await c.GetAsync<Smlouva>(idVerze, s => s.SourceIncludes(elasticPropertyPath));

                if (res.Found == false)
                {
                    if (specClient == false)
                    {
                        var c1 = await HlidacStatu.Connectors.Manager.GetESClient_SneplatneAsync();

                        res = await c1.GetAsync<Smlouva>(idVerze, s => s.SourceIncludes(elasticPropertyPath));

                        if (res.IsValid == false)
                        {
                            /*_logger.Warning("Valid Req: Cannot load Smlouva Id " + idVerze +
                                                     "\nDebug:" + res.DebugInformation);*/
                            //DirectDB.NoResult("delete from SmlouvyIds where id = @id", new System.Data.SqlClient.SqlParameter("id", idVerze));
                        }
                        else if (res.Found == false)
                            return null;
                        else if (res?.ServerError?.Status == 404)
                            return null;

                    }

                }
                if (res?.Source != null)
                {
                    JObject json = JObject.Parse(JsonConvert.SerializeObject(res.Source));
                    //Type itemType = typeof(T).GetElementType();
                    T[] foundProps = json.SelectTokens(jsonPath)
                                .Select(m => m.Value<T>())
                                .ToArray();

                    return foundProps;
                }
                return null;
            }
            catch (Exception e)
            {
                //_logger.Error(e, "Cannot load Smlouva Id " + idVerze);
                return null;
            }
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
                await SaveAsync(s, null, false, fireOCRDone: false);
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
                    c = await Manager.GetESClientAsync();

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
                        var c1 = await Manager.GetESClient_SneplatneAsync();

                        res = includePrilohy
                            ? await c1.GetAsync<Smlouva>(idVerze)
                            : await c1.GetAsync<Smlouva>(idVerze, s => s.SourceExcludes("prilohy.plainTextContent"));

                        if (res.Found)
                            return res.Source;
                        else if (res.IsValid)
                        {
                            _logger.Warning("Valid Req: Cannot load Smlouva Id " + idVerze +
                                                     "\nDebug:" + res.DebugInformation);
                            //DirectDB.NoResult("delete from SmlouvyIds where id = @id", new System.Data.SqlClient.SqlParameter("id", idVerze));
                        }
                        else if (res.Found == false)
                            return null;
                        else if (res.ServerError.Status == 404)
                            return null;
                        else
                        {
                            _logger.Error(
                                "Invalid Req: Cannot load Smlouva Id " + idVerze + "\n Debug:" + res.DebugInformation +
                                " \nServerError:" + res.ServerError?.ToString(), res.OriginalException);
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Cannot load Smlouva Id " + idVerze);
                return null;
            }
        }

    }
}