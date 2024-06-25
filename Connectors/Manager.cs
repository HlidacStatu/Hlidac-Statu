using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
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
            KIndex, KIndexBackup, KindexFeedback,
            KIndexTemp, KIndexBackupTemp,
            VerejneZakazky,
            ProfilZadavatele,
            VerejneZakazkyRaw2006,
            VerejneZakazkyRaw,
            VerejneZakazkyNaProfiluRaw,
            Logs,
            //DataSourceDb,
            DataSource,
            Insolvence,
            InsolvenceDocs,
            Dotace,
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
            PermanentLLM
        }

        public static string defaultIndexName = "hlidacsmluv";
        public static string defaultIndexName_Sneplatne = "hlidacsmluvneplatne";
        public static string defaultIndexName_SAll = defaultIndexName + "," + defaultIndexName_Sneplatne;

        public static string defaultIndexName_VerejneZakazky = "verejnezakazky";
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
        //public static string defaultIndexName_DataSourceDb = "hlidacstatu_datasources";
        public static string defaultIndexName_Insolvence = "insolvencnirestrik";
        public static string defaultIndexName_InsolvenceDocs = "insolvencedocs";
        public static string defaultIndexName_Dotace = "dotace";
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

        public static string defaultIndexName_SplitSmlouvy = "splitsmlouvy";
        public static string defaultIndexName_SearchPromo = "searchpromo";
        public static string defaultIndexName_PermanentLLM = "permanentllm";


        private static SemaphoreSlim _clientSemaphore = new SemaphoreSlim(1, 1);
        private static Dictionary<string, ElasticClient> _clients = new Dictionary<string, ElasticClient>();


        static Manager()
        {
            if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("DefaultIndexName")))
                defaultIndexName = Devmasters.Config.GetWebConfigValue("DefaultIndexName");
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
        }

        public static async Task InitElasticSearchIndexAsync(ElasticClient client, IndexType? idxType)
        {
            if (idxType == null)
                return;
            if (idxType.Value == IndexType.DataSource)
                return;
            var ret = await client.Indices.ExistsAsync(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists == false)
                await CreateIndexAsync(client, idxType.Value);

        }
        public static void DeleteIndex()
        {
            //GetESClient().DeleteIndex(defaultIndexName);
        }
        public static Task<ElasticClient> GetESClientAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName, timeOut, connectionLimit);
        }
        public static Task<ElasticClient> GetESClient_SneplatneAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Sneplatne, timeOut, connectionLimit);
        }
        
        [Obsolete(message: "Použij GetESClient_VerejneZakazkyAsync")]
        public static Task<ElasticClient> GetESClient_VZAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_VerejneZakazky, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }
        public static Task<ElasticClient> GetESClient_VerejneZakazkyAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            // workaround to get to the hidden index
            // string indexName = "hs-verejnezakazky_new-02";
            // return HackClientIndex(indexName, timeOut, connectionLimit);
            
            return GetESClientAsync(defaultIndexName_VerejneZakazkyNew, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }

        private static async Task<ElasticClient> HackClientIndex(string indexName, int timeOut, int connectionLimit)
        {
            string cnnset = string.Format("{0}|{1}|{2}", indexName, timeOut, connectionLimit);
            ConnectionSettings sett = GetElasticSearchConnectionSettings(indexName, timeOut, connectionLimit);

            if (!_clients.ContainsKey(cnnset))
            {
                await _clientSemaphore.WaitAsync();
                try
                {
                    if (!_clients.ContainsKey(cnnset))
                    {
                        //if (idxType.HasValue == false)
                        //    idxType = GetIndexTypeForDefaultIndexName(indexName);

                        var client = new ElasticClient(sett);
                        //await InitElasticSearchIndexAsync(_client, IndexType.VerejneZakazky);
                        var ret = await client.Indices.ExistsAsync(client.ConnectionSettings.DefaultIndex);
                        if (ret.Exists == false)
                            await CreateIndexAsync(client, IndexType.VerejneZakazky, withAlias: false);

                        _clients.TryAdd(cnnset, client);
                    }
                }
                finally
                {
                    _clientSemaphore.Release();
                }
            }

            return _clients[cnnset];
        }
        
        public static Task<ElasticClient> GetESClient_VerejneZakazkyOldAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_VerejneZakazky, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }
        
        public static Task<ElasticClient> GetESClient_ProfilZadavateleAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_ProfilZadavatele, timeOut, connectionLimit, IndexType.ProfilZadavatele);
        }
        public static Task<ElasticClient> GetESClient_VerejneZakazkyNaProfiluRawAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_VerejneZakazkyNaProfiluRaw, timeOut, connectionLimit, IndexType.VerejneZakazkyNaProfiluRaw);
        }
        
        public static Task<ElasticClient> GetESClient_LogsAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Logs, timeOut, connectionLimit, IndexType.Logs
                );
        }
        public static Task<ElasticClient> GetESClient_DocTablesAsync(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_DocTables, timeOut, connectionLimit, IndexType.DocTables
                );
        }
        public static Task<ElasticClient> GetESClient_InDocTableCellsAsync(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_InDocTableCells, timeOut, connectionLimit, IndexType.InDocTableCells
            );
        }
        public static Task<ElasticClient> GetESClient_RPP_OVMAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_RPP_OVM, timeOut, connectionLimit, IndexType.RPP_OVM
                );
        }
        public static Task<ElasticClient> GetESClient_RPP_ISVSAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_RPP_ISVS, timeOut, connectionLimit, IndexType.RPP_ISVS
                );
        }
        public static Task<ElasticClient> GetESClient_RPP_KategorieAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_RPP_Kategorie, timeOut, connectionLimit, IndexType.RPP_Kategorie
                );
        }

        public static Task<ElasticClient> GetESClient_InsolvenceAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Insolvence, timeOut, connectionLimit, IndexType.Insolvence);
        }
        public static Task<ElasticClient> GetESClient_InsolvenceDocsAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_InsolvenceDocs, timeOut, connectionLimit, IndexType.InsolvenceDocs);
        }

        public static Task<ElasticClient> GetESClient_SplitSmlouvyAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_SplitSmlouvy, timeOut, connectionLimit, IndexType.SplitSmlouvy);
        }
        public static Task<ElasticClient> GetESClient_SearchPromoAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_SearchPromo, timeOut, connectionLimit, IndexType.SearchPromo);
        }

        public static Task<ElasticClient> GetESClient_PermanentLLMAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_PermanentLLM, timeOut, connectionLimit, IndexType.PermanentLLM);
        }

        //public static ElasticClient GetESClient_Uptime(int timeOut = 60000, int connectionLimit = 80)
        //{
        //    return GetESClient(defaultIndexName_Uptime, timeOut, connectionLimit, IndexType.UptimeItem);
        //}

        public static Task<ElasticClient> GetESClient_PageMetadataAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_PageMetadata, timeOut, connectionLimit, IndexType.PageMetadata);
        }

        public static Task<ElasticClient> GetESClient_UptimeSSLAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_UptimeSSL, timeOut, connectionLimit, IndexType.UptimeSSL);
        }

        public static Task<ElasticClient> GetESClient_DotaceAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Dotace, timeOut, connectionLimit, IndexType.Dotace);
        }

        public static Task<ElasticClient> GetESClient_OsobyAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Osoby, timeOut, connectionLimit, IndexType.Osoby);
        }

        public static Task<ElasticClient> GetESClient_FirmyAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_Firmy, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static Task<ElasticClient> GetESClient_KIndexAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_KIndex, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static Task<ElasticClient> GetESClient_KIndexTempAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_KIndexTemp, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static Task<ElasticClient> GetESClient_KIndexBackupAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_KIndexBackup, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static Task<ElasticClient> GetESClient_KIndexBackupTempAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_KIndexBackupTemp, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static Task<ElasticClient> GetESClient_KindexFeedbackAsync(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClientAsync(defaultIndexName_KindexFeedback, timeOut, connectionLimit, IndexType.KindexFeedback);
        }

        public const string DataSourceIndexNamePrefix = "data_";
        public static async Task<ElasticClient> GetESClientAsync(string indexName, int timeOut = 60000, int connectionLimit = 80, IndexType? idxType = null, bool init = true)
        {
            if (idxType == IndexType.DataSource)
                indexName = DataSourceIndexNamePrefix + indexName;
            else if (indexName == defaultIndexName_Audit)
            {
                //audit_Year-weekInYear
                DateTime d = DateTime.Now;
                indexName = $"{indexName}_{d.Year}-{CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
            }
            string cnnset = string.Format("{0}|{1}|{2}", indexName, timeOut, connectionLimit);

            //if (System.Diagnostics.Debugger.IsAttached)
            //    sett.Proxy(new Uri("http://127.0.0.1:8888"),"","");

            if (!_clients.ContainsKey(cnnset))
            {
                await _clientSemaphore.WaitAsync();
                try
                {
                    if (!_clients.ContainsKey(cnnset))
                    {
                        //if (idxType.HasValue == false)
                        //    idxType = GetIndexTypeForDefaultIndexName(indexName);

                        ConnectionSettings sett = GetElasticSearchConnectionSettings(indexName, timeOut, connectionLimit);
                        var client = new ElasticClient(sett);
                        if (init)
                            await InitElasticSearchIndexAsync(client, idxType);

                        _clients.TryAdd(cnnset, client);
                    }
                }
                finally
                {
                    _clientSemaphore.Release();
                }
            }
            return _clients[cnnset];

        }


        public static ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000, int? connectionLimit = null)
        {

            string esUrl = Devmasters.Config.GetWebConfigValue("ESConnection");

            // var singlePool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(esUrl.Split(';').First()));
            var pool = new Elasticsearch.Net.StaticConnectionPool(esUrl
                .Split(';')
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(u => new Uri(u))
                );

            //var pool = new Elasticsearch.Net.SniffingConnectionPool(esUrl
            //    .Split(';')
            //    .Where(m=>!string.IsNullOrWhiteSpace(m))
            //    .Select(u => new Uri(u)));
            var settings = new ConnectionSettings(pool)
                .DefaultIndex(indexName)
                .DisableAutomaticProxyDetection(false)
                .RequestTimeout(TimeSpan.FromMilliseconds(timeOut))
                .SniffLifeSpan(null)
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

            if (System.Diagnostics.Debugger.IsAttached || Devmasters.Config.GetWebConfigValue("ESDebugDataEnabled") == "true")
                settings = settings.DisableDirectStreaming();

            if (connectionLimit.HasValue)
                settings = settings.ConnectionLimit(connectionLimit.Value);

            //.ConnectionLimit(connectionLimit)
            //.MaximumRetries(2)

            //.SetProxy(new Uri("http://localhost.fiddler:8888"), "", "")


            return settings;
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source,
    Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expresion '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return propInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="props"></param>
        /// <returns>true if changed at least one property</returns>
        public static bool AddMissingPropertyValuesToFirst<T>(ref T first, ref T second, params Expression<Func<T, object>>[] props)
        {
            //http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression
            //http://stackoverflow.com/questions/9516235/there-is-a-way-to-update-all-properties-of-an-object-changing-only-it-values

            bool changed = false;
            List<string> propNames = new List<string>();
            propNames = props
                .Select(p => (MemberExpression)(p.Body as MemberExpression))
                .Where(p => p != null)
                .Select(m => m.Member.Name)
                .ToList();

            foreach (var propName in propNames)
            {
                PropertyInfo propertyInfo = first.GetType().GetProperty(propName);
                object newPropVal = propertyInfo.GetValue(second);
                object oldPropVal = propertyInfo.GetValue(first);
                bool isNullable = (Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null);

                if (isNullable)
                {
                    if (oldPropVal == null && newPropVal != null)
                    {
                        propertyInfo.SetValue(first, newPropVal);
                        changed = true;
                    }
                }

                object defValue = GetDefault(propertyInfo.PropertyType);
                if (oldPropVal == defValue && newPropVal != defValue)
                {
                    propertyInfo.SetValue(first, newPropVal);
                    changed = true;
                }
            }
            return changed;
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }


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

        public static async Task CreateIndexAsync(ElasticClient client, IndexType idxTyp, bool withAlias = true)
        {
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
            var aliasName = client.ConnectionSettings.DefaultIndex.ToLower();
            var indexName = (withAlias ? $"hs-{aliasName}-01" : aliasName).ToLower();
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
                           .Map<HlidacStatu.Entities.PermanentLLM.Summary>(map => map.AutoMap().DateDetection(false))  //TODO Summary to T or Object
                       );
                    break;
                case IndexType.Dotace:
                    res = await client.Indices
                       .CreateAsync(indexName, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.Dotace.Dotace>(map => map.AutoMap().DateDetection(false))
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
            
            if(withAlias)
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

        public static void LogQueryError<T>(ISearchResponse<T> esReq, string text = "", HttpContext httpContext = null, Exception ex = null)
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

        public static Dictionary<string, ElasticClient> GetConnectionPool()
        {
            return _clients;
        }

    }
}
