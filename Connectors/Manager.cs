using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Lib.Data.External.RPP;
using HlidacStatu.Repositories.ProfilZadavatelu;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Nest;
using Serilog;

namespace HlidacStatu.Connectors
{
    public class Manager
    {
        public const int MaxResultWindow = 10000;

        private static readonly ILogger _logger = Log.ForContext<Manager>();

        public enum IndexType
        {
            Smlouvy,
            Firmy,
            KIndex,
            KIndexBackup,
            KindexFeedback,
            KIndexTemp,
            KIndexBackupTemp,
            VerejneZakazky,
            ProfilZadavatele,
            VerejneZakazkyNaProfiluRaw,
            Logs,
            DataSource,
            Insolvence,
            InsolvenceDocs,
            Subsidy,
            DotacniVyzva,
            UptimeItem,
            UptimeSSL,
            PageMetadata,
            Osoby,
            DocTables,
            RPP_Kategorie,
            RPP_OVM,
            RPP_ISVS,
            InDocTableCells,
            DocumentHistory,
            SplitSmlouvy,
            SearchPromo,
            PermanentLLM,
            Dotace,
            AuditLog,
            STK
        }

        public static string defaultIndexName = "hlidacsmluv";
        public static string defaultIndexName_Sneplatne = "hlidacsmluvneplatne";
        public static string defaultIndexName_SAll = defaultIndexName + "," + defaultIndexName_Sneplatne;
        public static string defaultIndexName_VerejneZakazkyNew = "verejnezakazky_new";
        public static string defaultIndexName_ProfilZadavatele = "profilzadavatele";
        public static string defaultIndexName_VerejneZakazkyNaProfiluRaw = "verejnezakazkyprofilraw";
        public static string defaultIndexName_Firmy = "firmy";
        public static string defaultIndexName_KIndex = "kindex";
        public static string defaultIndexName_KIndexTemp = "kindex_temp";
        public static string defaultIndexName_KIndexBackup = "kindexbackup";
        public static string defaultIndexName_KIndexBackupTemp = "kindexbackup_temp";
        public static string defaultIndexName_KindexFeedback = "kindexfeedback";
        public static string defaultIndexName_Logs = "logs";
        public static string defaultIndexName_Insolvence = "insolvencnirestrik";
        public static string defaultIndexName_InsolvenceDocs = "insolvencedocs";
        public static string defaultIndexName_Subsidy = "subsidy3";
        public static string defaultIndexName_DotacniVyzva = "dotacnivyzva";
        public static string defaultIndexName_Dotace = "dotace3";
        public static string defaultIndexName_AuditLog = "audit_log";
        public static string defaultIndexName_DotaceOld = "dotace";
        public static string defaultIndexName_Uptime = "uptime";
        public static string defaultIndexName_UptimeSSL = "uptimessl";
        public static string defaultIndexName_PageMetadata = "pagemetadata";
        public static string defaultIndexName_Audit = "audit";
        public static string defaultIndexName_Osoby = "osoby";
        public static string defaultIndexName_DocTables = "doctables";
        public static string defaultIndexName_InDocTableCells = "indoctablecells";
        public static string defaultIndexName_RPP_Kategorie = "rpp_kategorie";
        public static string defaultIndexName_RPP_OVM = "rpp_ovm";
        public static string defaultIndexName_RPP_ISVS = "rpp_isvs";
        public static string defaultIndexName_STK = "stk";
        public static string defaultIndexName_SplitSmlouvy = "splitsmlouvy";
        public static string defaultIndexName_SearchPromo = "searchpromo";
        public static string defaultIndexName_PermanentLLM = "permanentllm";


        private static readonly ConcurrentDictionary<string, ElasticClient> _clients = new();


        static Manager()
        {
            if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("DefaultIndexName")))
                defaultIndexName = Devmasters.Config.GetWebConfigValue("DefaultIndexName");
            
            // Fire and forget - initialize indices in background
            // pragmatic approach since most of indices are already created
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeAllIndicesAsync();
                    _logger.Information("Elasticsearch indices initialized successfully during startup");
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex, "Failed to initialize Elasticsearch indices during startup");
                }
            });
        }

        public static ElasticClient GetESClient(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName, timeOut, connectionLimit);
        }

        public static ElasticClient GetESClient_Sneplatne(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Sneplatne, timeOut, connectionLimit);
        }

        public static ElasticClient GetESClient_VerejneZakazky(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyNew, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }

        public static ElasticClient GetESClient_ProfilZadavatele(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_ProfilZadavatele, timeOut, connectionLimit, IndexType.ProfilZadavatele);
        }

        public static ElasticClient GetESClient_VerejneZakazkyNaProfiluRaw(int timeOut = 60000,
            int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyNaProfiluRaw, timeOut, connectionLimit,
                IndexType.VerejneZakazkyNaProfiluRaw);
        }

        public static ElasticClient GetESClient_Logs(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Logs, timeOut, connectionLimit, IndexType.Logs);
        }

        public static ElasticClient GetESClient_DocTables(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_DocTables, timeOut, connectionLimit, IndexType.DocTables);
        }

        public static ElasticClient GetESClient_InDocTableCells(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_InDocTableCells, timeOut, connectionLimit, IndexType.InDocTableCells);
        }

        public static ElasticClient GetESClient_RPP_OVM(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_OVM, timeOut, connectionLimit, IndexType.RPP_OVM);
        }
        public static ElasticClient GetESClient_STK(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_STK, timeOut, connectionLimit, IndexType.STK);
        }

        public static ElasticClient GetESClient_RPP_ISVS(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_ISVS, timeOut, connectionLimit, IndexType.RPP_ISVS);
        }

        public static ElasticClient GetESClient_RPP_Kategorie(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_Kategorie, timeOut, connectionLimit, IndexType.RPP_Kategorie);
        }

        public static ElasticClient GetESClient_Insolvence(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Insolvence, timeOut, connectionLimit, IndexType.Insolvence);
        }

        public static ElasticClient GetESClient_InsolvenceDocs(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_InsolvenceDocs, timeOut, connectionLimit, IndexType.InsolvenceDocs);
        }

        public static ElasticClient GetESClient_SplitSmlouvy(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_SplitSmlouvy, timeOut, connectionLimit, IndexType.SplitSmlouvy);
        }

        public static ElasticClient GetESClient_SearchPromo(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_SearchPromo, timeOut, connectionLimit, IndexType.SearchPromo);
        }

        public static ElasticClient GetESClient_PermanentLLM(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_PermanentLLM, timeOut, connectionLimit, IndexType.PermanentLLM);
        }

        public static ElasticClient GetESClient_PageMetadata(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_PageMetadata, timeOut, connectionLimit, IndexType.PageMetadata);
        }

        public static ElasticClient GetESClient_UptimeSSL(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_UptimeSSL, timeOut, connectionLimit, IndexType.UptimeSSL);
        }

        public static ElasticClient GetESClient_Subsidy(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Subsidy, timeOut, connectionLimit, IndexType.Subsidy);
        }

        public static ElasticClient GetESClient_DotacniVyzva(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_DotacniVyzva, timeOut, connectionLimit, IndexType.DotacniVyzva);
        }

        public static ElasticClient GetESClient_Dotace(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Dotace, timeOut, connectionLimit, IndexType.Dotace);
        }

        public static ElasticClient GetESClient_DotaceOld(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_DotaceOld, timeOut, connectionLimit, IndexType.Dotace);
        }

        public static ElasticClient GetESClient_Osoby(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Osoby, timeOut, connectionLimit, IndexType.Osoby);
        }

        public static ElasticClient GetESClient_Firmy(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Firmy, timeOut, connectionLimit, IndexType.Firmy);
        }

        public static ElasticClient GetESClient_KIndex(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndex, timeOut, connectionLimit, IndexType.KIndex);
        }

        public static ElasticClient GetESClient_KIndexTemp(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexTemp, timeOut, connectionLimit, IndexType.KIndexTemp);
        }

        public static ElasticClient GetESClient_KIndexBackup(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexBackup, timeOut, connectionLimit, IndexType.KIndexBackup);
        }

        public static ElasticClient GetESClient_KIndexBackupTemp(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexBackupTemp, timeOut, connectionLimit, IndexType.KIndexBackupTemp);
        }

        public static ElasticClient GetESClient_KindexFeedback(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KindexFeedback, timeOut, connectionLimit, IndexType.KindexFeedback);
        }

        public static ElasticClient GetESClient_AuditLog(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_AuditLog, timeOut, connectionLimit, IndexType.AuditLog);
        }

        public const string DataSourceIndexNamePrefix = "data_";

        // Internal method that doesn't wait for initialization - used during initialization itself
        public static ElasticClient GetESClient(string indexName, int timeOut = 60000,
            int connectionLimit = 80, IndexType? idxType = null)
        {
            if (idxType == IndexType.DataSource)
                indexName = DataSourceIndexNamePrefix + indexName;
            else if (indexName == defaultIndexName_Audit)
            {
                DateTime d = DateTime.Now;
                indexName =
                    $"{indexName}_{d.Year}-{CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
            }

            string cnnset = $"{indexName}|{timeOut}|{connectionLimit}";

            return _clients.GetOrAdd(cnnset, key =>
            {
                ConnectionSettings sett = GetElasticSearchConnectionSettings(indexName, timeOut, connectionLimit);
                return new ElasticClient(sett);
            });
        }
        

        public static async Task InitializeAllIndicesAsync()
        {
            _logger.Information("Starting Elasticsearch indices initialization...");

            var initTasks = new List<Task>
            {
                InitializeIndexAsync(IndexType.Smlouvy, defaultIndexName),
                InitializeIndexAsync(IndexType.Firmy, defaultIndexName_Firmy),
                InitializeIndexAsync(IndexType.KIndex, defaultIndexName_KIndex),
                InitializeIndexAsync(IndexType.KIndexTemp, defaultIndexName_KIndexTemp),
                InitializeIndexAsync(IndexType.KIndexBackup, defaultIndexName_KIndexBackup),
                InitializeIndexAsync(IndexType.KIndexBackupTemp, defaultIndexName_KIndexBackupTemp),
                InitializeIndexAsync(IndexType.KindexFeedback, defaultIndexName_KindexFeedback),
                InitializeIndexAsync(IndexType.VerejneZakazky, defaultIndexName_VerejneZakazkyNew),
                InitializeIndexAsync(IndexType.ProfilZadavatele, defaultIndexName_ProfilZadavatele),
                InitializeIndexAsync(IndexType.VerejneZakazkyNaProfiluRaw, defaultIndexName_VerejneZakazkyNaProfiluRaw),
                InitializeIndexAsync(IndexType.Logs, defaultIndexName_Logs),
                InitializeIndexAsync(IndexType.Insolvence, defaultIndexName_Insolvence),
                InitializeIndexAsync(IndexType.InsolvenceDocs, defaultIndexName_InsolvenceDocs),
                InitializeIndexAsync(IndexType.Subsidy, defaultIndexName_Subsidy),
                InitializeIndexAsync(IndexType.DotacniVyzva, defaultIndexName_DotacniVyzva),
                InitializeIndexAsync(IndexType.Dotace, defaultIndexName_Dotace),
                InitializeIndexAsync(IndexType.AuditLog, defaultIndexName_AuditLog),
                InitializeIndexAsync(IndexType.UptimeSSL, defaultIndexName_UptimeSSL),
                InitializeIndexAsync(IndexType.PageMetadata, defaultIndexName_PageMetadata),
                InitializeIndexAsync(IndexType.Osoby, defaultIndexName_Osoby),
                InitializeIndexAsync(IndexType.DocTables, defaultIndexName_DocTables),
                InitializeIndexAsync(IndexType.InDocTableCells, defaultIndexName_InDocTableCells),
                InitializeIndexAsync(IndexType.RPP_Kategorie, defaultIndexName_RPP_Kategorie),
                InitializeIndexAsync(IndexType.RPP_OVM, defaultIndexName_RPP_OVM),
                InitializeIndexAsync(IndexType.RPP_ISVS, defaultIndexName_RPP_ISVS),
                InitializeIndexAsync(IndexType.STK, defaultIndexName_STK),
                InitializeIndexAsync(IndexType.SplitSmlouvy, defaultIndexName_SplitSmlouvy),
                InitializeIndexAsync(IndexType.SearchPromo, defaultIndexName_SearchPromo),
                InitializeIndexAsync(IndexType.PermanentLLM, defaultIndexName_PermanentLLM),
            };

            await Task.WhenAll(initTasks);

            _logger.Information("Elasticsearch indices initialization completed.");
        }

        private static async Task InitializeIndexAsync(IndexType idxType, string indexName)
        {
            try
            {
                var client = GetESClient(indexName);
                await CreateIndexAsync(client, idxType);
                _logger.Debug($"Index {indexName} ({idxType}) initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to initialize index {indexName} ({idxType})");
                throw;
            }
        }

        public async static Task<(bool success, CreateIndexResponse response)> CheckAndCreateIndexAsync<T>(
            ElasticClient client,
            bool withAlias = true,
            IndexSettings indexSetting = null,
            Nest.Analysis indexAnalysis = null
            ) where T : class
        {
            var aliasName = client.ConnectionSettings.DefaultIndex.ToLower();
            var indexName = (withAlias ? $"hs-{aliasName}-01" : aliasName).ToLower();


            //check if index already exists
            var indexExist = await client.Indices.ExistsAsync(indexName);
            if (indexExist?.Exists == true)
            {
                return (true,null);
            }

            indexSetting = indexSetting ?? new IndexSettings()
            {
                NumberOfReplicas = 1,
                NumberOfShards = 1,
                RefreshInterval = new Time(TimeSpan.FromSeconds(5))
            };
            indexAnalysis ??= new Nest.Analysis()
            {
                Analyzers = new Analyzers(),
                TokenFilters = BasicTokenFilters(),
            };

            indexSetting.Analysis = indexAnalysis;
            indexSetting.Analysis.Analyzers.Add("default", DefaultAnalyzer());

            IndexState idxSt = new IndexState();
            idxSt.Settings = indexSetting;

            CreateIndexResponse res = null;
            res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<T>(map => map.AutoMap().DateDetection(false))
                        );
            if (res.IsValid && withAlias)
                await client.Indices.PutAliasAsync(indexName, aliasName);

            return (res?.IsValid ?? false, res);
        }

        public static ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000,
            int? connectionLimit = null)
        {
            string esUrl = Devmasters.Config.GetWebConfigValue("ESConnection");

            var pool = new Elasticsearch.Net.StickyConnectionPool(esUrl
                .Split(';')
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(u => new Uri(u))
            );

            var settings = new ConnectionSettings(pool)
                    .DefaultIndex(indexName)
                    .DisableAutomaticProxyDetection(false)
                    .RequestTimeout(TimeSpan.FromMilliseconds(timeOut))
                    //.SniffLifeSpan(null)
                    .SniffOnStartup(true)
                    .OnRequestCompleted(call =>
                    {
                        // log out the request and the request body, if one exists for the type of request
                        if (call.RequestBodyInBytes != null)
                        {
                            _logger.Debug($"{call.HttpMethod}\t{call.Uri}\t" +
                                          $"{Encoding.UTF8.GetString(call.RequestBodyInBytes)}");
                        }
                        else
                        {
                            _logger.Debug($"{call.HttpMethod}\t{call.Uri}\t");
                        }
                    })
                ;

            if (System.Diagnostics.Debugger.IsAttached ||
                Devmasters.Config.GetWebConfigValue("ESDebugDataEnabled") == "true")
                settings = settings.DisableDirectStreaming();

            if (connectionLimit.HasValue)
                settings = settings.ConnectionLimit(connectionLimit.Value);

            return settings;
        }

        //datasety
        public static async Task CreateIndexAsync(ElasticClient client)
        {
            IndexSettings set = new IndexSettings();
            set.NumberOfReplicas = 1;
            set.NumberOfShards = 1;

            // Add the Analyzer with a name
            set.Analysis = new Nest.Analysis()
            {
                Analyzers = new Analyzers(),
                TokenFilters = BasicTokenFilters(),
            };

            set.Analysis.Analyzers.Add("default", DefaultAnalyzer());

            IndexState idxSt = new IndexState();
            idxSt.Settings = set;

            var aliasName = client.ConnectionSettings.DefaultIndex;
            var indexName = $"hs-{aliasName}-01";
            await client.Indices
                .CreateAsync(indexName, i => i
                    .InitializeUsing(idxSt)
                    .Map(mm => mm
                        .Properties(ps => ps
                            .Date(psn => psn.Name("DbCreated"))
                            .Keyword(psn => psn.Name("DbCreatedBy"))
                        )
                    )
                );
            await client.Indices.PutAliasAsync(indexName, aliasName);
        }

        private static async Task CreateIndexAsync(ElasticClient client, IndexType idxTyp, bool withAlias = true)
        {
            var aliasName = client.ConnectionSettings.DefaultIndex.ToLower();
            var indexName = (withAlias ? $"hs-{aliasName}-01" : aliasName).ToLower();

            //check if index already exists
            var indexExist = await client.Indices.ExistsAsync(indexName);
            if (indexExist?.Exists == true)
            {
                return;
            }

            IndexSettings set = new IndexSettings();
            set.NumberOfReplicas = 1;
            set.NumberOfShards = 1;
            set.RefreshInterval = new Time(TimeSpan.FromSeconds(1));


            // Add the Analyzer with a name
            set.Analysis = new Nest.Analysis()
            {
                Analyzers = new Analyzers(),
                TokenFilters = BasicTokenFilters(),
            };

            set.Analysis.Analyzers.Add("default", DefaultAnalyzer());

            IndexState idxSt = new IndexState();
            idxSt.Settings = set;

            CreateIndexResponse res = null;

            switch (idxTyp)
            {
                case IndexType.VerejneZakazky:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.VZ.VerejnaZakazka>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.DocumentHistory:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.DocumentHistory<IDocumentHash>>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.ProfilZadavatele:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.VZ.ProfilZadavatele>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.Insolvence:
                    idxSt.Settings.NumberOfShards = 16;
                    idxSt.Settings.RefreshInterval = "5s";
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Insolvence.Rizeni>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.InsolvenceDocs:
                    idxSt.Settings.NumberOfShards = 8;
                    idxSt.Settings.RefreshInterval = "15s";
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Insolvence.SearchableDocument>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.SplitSmlouvy:
                    idxSt.Settings.NumberOfShards = 2;
                    idxSt.Settings.RefreshInterval = "15s";
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<HlidacStatu.MLUtil.Splitter.SplitSmlouva>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.SearchPromo:
                    idxSt.Settings.NumberOfShards = 2;
                    idxSt.Settings.RefreshInterval = "15s";
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<HlidacStatu.Entities.SearchPromo>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.PermanentLLM:
                    idxSt.Settings.NumberOfShards = 2;
                    idxSt.Settings.RefreshInterval = "15s";
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                                .InitializeUsing(idxSt)
                                .Map<HlidacStatu.Entities.AI.FullSummary>(map =>
                                    map.AutoMap().DateDetection(false)) //TODO Summary to T or Object
                        );
                    break;
                case IndexType.Subsidy:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Subsidy>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.DotacniVyzva:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.DotacniVyzva>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.Dotace:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Dotace>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.AuditLog:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.AuditLog>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.PageMetadata:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.PageMetadata>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.UptimeSSL:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.UptimeSSL>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.UptimeItem:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.UptimeItem>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.Osoby:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(new IndexState()
                            {
                                Settings = new IndexSettings()
                                {
                                    NumberOfReplicas = 1,
                                    NumberOfShards = 1,
                                    Analysis = new Nest.Analysis()
                                    {
                                        TokenFilters = BasicTokenFilters(),
                                        Analyzers = new Analyzers(new Dictionary<string, IAnalyzer>()
                                        {
                                            ["default"] = DefaultAnalyzer(),
                                            ["lowercase"] = LowerCaseOnlyAnalyzer(),
                                            ["lowercase_ascii"] = LowerCaseAsciiAnalyzer()
                                        })
                                    }
                                }
                            })
                            .Map<Entities.OsobyES.OsobaES>(map => map
                                .AutoMap()
                                .Properties(p => p
                                    .Text(t => t
                                        .Name(n => n.FullName)
                                        .Fields(ff => ff
                                            .Text(tt => tt
                                                .Name("lower")
                                                .Analyzer("lowercase")
                                            )
                                            .Text(tt => tt
                                                .Name("lowerascii")
                                                .Analyzer("lowercase_ascii")
                                            )
                                        )
                                    )
                                )
                            )
                        );
                    break;

                case IndexType.Smlouvy:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Smlouva>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
                case IndexType.Firmy:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.FirmaInElastic>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.KIndex:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<KIndexData>(map => map.AutoMap(maxRecursion: 2))
                        );
                    break;
                case IndexType.KIndexTemp:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<KIndexData>(map => map.AutoMap(maxRecursion: 2))
                        );
                    break;
                case IndexType.KIndexBackup:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Backup>(map => map.AutoMap(maxRecursion: 2))
                        );
                    break;
                case IndexType.KIndexBackupTemp:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Backup>(map => map.AutoMap(maxRecursion: 2))
                        );
                    break;
                case IndexType.KindexFeedback:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<KindexFeedback>(map => map.AutoMap(maxRecursion: 2))
                        );
                    break;
                case IndexType.Logs:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.Logs.ProfilZadavateleDownload>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.DocTables:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(new IndexState()
                                {
                                    Settings = new IndexSettings()
                                    {
                                        NumberOfReplicas = 1,
                                        NumberOfShards = 8,
                                        RefreshInterval = new Time(TimeSpan.FromSeconds(1))
                                    }
                                }
                            )
                            .Map<Entities.DocTables>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.VerejneZakazkyNaProfiluRaw:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<ZakazkaRaw>(map => map
                                .Properties(p => p
                                    .Keyword(k => k.Name(n => n.ZakazkaId))
                                    .Keyword(k => k.Name(n => n.Profil))
                                    .Date(k => k.Name(n => n.LastUpdate))
                                )
                            )
                        );
                    break;
                case IndexType.RPP_Kategorie:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(new IndexState()
                                {
                                    Settings = new IndexSettings()
                                    {
                                        NumberOfReplicas = 1,
                                        NumberOfShards = 1
                                    }
                                }
                            )
                            .Map<KategorieOVM>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.RPP_OVM:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(new IndexState()
                                {
                                    Settings = new IndexSettings()
                                    {
                                        NumberOfReplicas = 1,
                                        NumberOfShards = 1
                                    }
                                }
                            )
                            .Map<OVMFull>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.STK:
                    //use
                    break;
                case IndexType.RPP_ISVS:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(new IndexState()
                                {
                                    Settings = new IndexSettings()
                                    {
                                        NumberOfReplicas = 1,
                                        NumberOfShards = 1
                                    }
                                }
                            )
                            .Map<ISVS>(map => map.AutoMap(maxRecursion: 1))
                        );
                    break;
                case IndexType.InDocTableCells:
                    res = await client.Indices
                        .CreateAsync(indexName, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.InDocTableCells>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
            }

            if (withAlias)
                await client.Indices.PutAliasAsync(indexName, aliasName);
        }

        private static ITokenFilters BasicTokenFilters()
        {
            var tokenFilters = new TokenFilters();
            tokenFilters.Add("czech_stop", new StopTokenFilter() { StopWords = new string[] { "_czech_" } });
            tokenFilters.Add("czech_stemmer", new StemmerTokenFilter() { Language = "czech" });
            return tokenFilters;
        }

        private static IAnalyzer DefaultAnalyzer()
        {
            var analyzer = new CustomAnalyzer();
            analyzer.Tokenizer = "standard";
            analyzer.Filter = new List<string>
            {
                "lowercase",
                "czech_stop",
                "czech_stemmer",
                "asciifolding"
            };
            return analyzer;
        }

        private static IAnalyzer LowerCaseOnlyAnalyzer()
        {
            var analyzer = new CustomAnalyzer();
            analyzer.Tokenizer = "whitespace";
            analyzer.Filter = new List<string>
            {
                "lowercase",
            };
            return analyzer;
        }

        private static IAnalyzer LowerCaseAsciiAnalyzer()
        {
            var analyzer = new CustomAnalyzer();
            analyzer.Tokenizer = "whitespace";
            analyzer.Filter = new List<string>
            {
                "lowercase",
                "asciifolding"
            };
            return analyzer;
        }

        public static void LogQueryError<T>(ISearchResponse<T> esReq, string text = "", HttpContext httpContext = null,
            Exception ex = null)
            where T : class
        {
            StringValues browser = new StringValues();
            httpContext?.Request?.Headers?.TryGetValue("User-Agent", out browser);

            Elasticsearch.Net.ServerError serverErr = esReq.ServerError;
            _logger.Debug(ex, "ES query error: " + text
                                                 + "\n\nCause:" + serverErr?.Error?.ToString()
                                                 + "\n\nDetail:" + esReq.DebugInformation
                                                 + "\n\n"
                                                 + "\nURL {URL}"
                                                 + "\nStack-trace {Stack-trace}"
                                                 + "\nReferer {Referer}"
                                                 + "\nUser {User-agent}"
                                                 + "\nIP {IP}",
                httpContext?.Request?.GetDisplayUrl(),
                Environment.StackTrace,
                httpContext?.Request?.GetTypedHeaders()?.Referer?.ToString(),
                browser.ToString(),
                HlidacStatu.Util.RealIpAddress.GetIp(httpContext)?.ToString()
            );
        }
    }
}