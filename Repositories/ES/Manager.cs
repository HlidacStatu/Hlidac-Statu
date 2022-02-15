using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Lib.Data.External.RPP;
using HlidacStatu.Repositories.ProfilZadavatelu;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

using Nest;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace HlidacStatu.Repositories.ES
{
    public class Manager
    {

        public static Devmasters.Log.Logger ESTraceLogger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Lib.ES.Trace");
        public static Devmasters.Log.Logger ESLogger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Lib.ES",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                            );
        public static bool ESTraceLoggerExists = log4net.LogManager.Exists("HlidacStatu.Lib.ES.Trace")?.Logger?.IsEnabledFor(log4net.Core.Level.Debug) == true;

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
            Dotace,
            UptimeItem,
            UptimeSSL,
            Osoby,
            Audit,
            RPP_Kategorie,
            RPP_OVM,
            RPP_ISVS,
            InDocTableCells
        }

        public static string defaultIndexName = "hlidacsmluv";
        public static string defaultIndexName_Sneplatne = "hlidacsmluvneplatne";
        public static string defaultIndexName_SAll = defaultIndexName + "," + defaultIndexName_Sneplatne;

        public static string defaultIndexName_VerejneZakazky = "verejnezakazky";
        public static string defaultIndexName_ProfilZadavatele = "profilzadavatele";
        public static string defaultIndexName_VerejneZakazkyRaw2006 = "verejnezakazkyraw2006";
        public static string defaultIndexName_VerejneZakazkyRaw = "verejnezakazkyraw";
        public static string defaultIndexName_VerejneZakazkyNaProfiluRaw = "verejnezakazkyprofilraw";
        public static string defaultIndexName_VerejneZakazkyNaProfiluConverted = "verejnezakazkyprofilconverted";
        public static string defaultIndexName_Firmy = "firmy";
        public static string defaultIndexName_KIndex = "kindex";
        public static string defaultIndexName_KIndexTemp = "kindex_temp";
        public static string defaultIndexName_KIndexBackup = "kindexbackup";
        public static string defaultIndexName_KIndexBackupTemp = "kindexbackup_temp";
        public static string defaultIndexName_KindexFeedback = "kindexfeedback";
        public static string defaultIndexName_Logs = "logs";
        //public static string defaultIndexName_DataSourceDb = "hlidacstatu_datasources";
        public static string defaultIndexName_Insolvence = "insolvencnirestrik";
        public static string defaultIndexName_Dotace = "dotace";
        public static string defaultIndexName_Uptime= "uptime";
        public static string defaultIndexName_UptimeSSL = "uptimessl";
        
        public static string defaultIndexName_Osoby = "osoby";
        public static string defaultIndexName_Audit = "audit";
        public static string defaultIndexName_InDocTableCells = "indoctablecells";

        public static string defaultIndexName_RPP_Kategorie = "rpp_kategorie";
        public static string defaultIndexName_RPP_OVM = "rpp_ovm";
        public static string defaultIndexName_RPP_ISVS = "rpp_isvs";

        private static object _clientLock = new object();
        private static Dictionary<string, ElasticClient> _clients = new Dictionary<string, ElasticClient>();

        private static object locker = new object();

        static Manager()
        {
            if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("DefaultIndexName")))
                defaultIndexName = Devmasters.Config.GetWebConfigValue("DefaultIndexName");
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
        }

        //public static void InitElasticSearchIndex()
        //{
        //    InitElasticSearchIndex(defaultIndexName);
        //}
        public static void InitElasticSearchIndex(ElasticClient client, IndexType? idxType)
        {
            if (idxType == null)
                return;
            if (idxType.Value == IndexType.DataSource)
                return;
            var ret = client.Indices.Exists(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists == false)
                CreateIndex(client, idxType.Value);

        }
        public static void DeleteIndex()
        {
            //GetESClient().DeleteIndex(defaultIndexName);
        }
        public static ElasticClient GetESClient(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName, timeOut, connectionLimit);
        }
        public static ElasticClient GetESClient_Sneplatne(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Sneplatne, timeOut, connectionLimit);
        }

        public static ElasticClient GetESClient_VZ(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazky, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }
        public static ElasticClient GetESClient_ProfilZadavatele(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_ProfilZadavatele, timeOut, connectionLimit, IndexType.ProfilZadavatele);
        }
        public static ElasticClient GetESClient_VerejneZakazkyRaw2006(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyRaw2006, timeOut, connectionLimit, IndexType.VerejneZakazkyRaw2006);
        }
        public static ElasticClient GetESClient_VerejneZakazkyNaProfiluRaw(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyNaProfiluRaw, timeOut, connectionLimit, IndexType.VerejneZakazkyNaProfiluRaw);
        }
        public static ElasticClient GetESClient_VerejneZakazkyNaProfiluConverted(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyNaProfiluConverted, timeOut, connectionLimit, IndexType.VerejneZakazky);
        }
        public static ElasticClient GetESClient_VerejneZakazkyRaw(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_VerejneZakazkyRaw, timeOut, connectionLimit, IndexType.VerejneZakazkyRaw
                );
        }
        public static ElasticClient GetESClient_Logs(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Logs, timeOut, connectionLimit, IndexType.Logs
                );
        }
        public static ElasticClient GetESClient_Audit(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Audit, timeOut, connectionLimit, IndexType.Audit
                );
        }
        public static ElasticClient GetESClient_InDocTableCells(int timeOut = 1000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_InDocTableCells, timeOut, connectionLimit, IndexType.InDocTableCells
            );
        }
        public static ElasticClient GetESClient_RPP_OVM(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_OVM, timeOut, connectionLimit, IndexType.RPP_OVM
                );
        }
        public static ElasticClient GetESClient_RPP_ISVS(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_ISVS, timeOut, connectionLimit, IndexType.RPP_ISVS
                );
        }
        public static ElasticClient GetESClient_RPP_Kategorie(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_RPP_Kategorie, timeOut, connectionLimit, IndexType.RPP_Kategorie
                );
        }

        public static ElasticClient GetESClient_Insolvence(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Insolvence, timeOut, connectionLimit, IndexType.Insolvence);
        }
        public static ElasticClient GetESClient_Uptime(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Uptime, timeOut, connectionLimit, IndexType.UptimeItem);
        }
        public static ElasticClient GetESClient_UptimeSSL(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_UptimeSSL, timeOut, connectionLimit, IndexType.UptimeSSL);
        }

        public static ElasticClient GetESClient_Dotace(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_Dotace, timeOut, connectionLimit, IndexType.Dotace);
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
            return GetESClient(defaultIndexName_KIndex, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static ElasticClient GetESClient_KIndexTemp(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexTemp, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static ElasticClient GetESClient_KIndexBackup(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexBackup, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static ElasticClient GetESClient_KIndexBackupTemp(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KIndexBackupTemp, timeOut, connectionLimit, IndexType.Firmy);
        }
        public static ElasticClient GetESClient_KindexFeedback(int timeOut = 60000, int connectionLimit = 80)
        {
            return GetESClient(defaultIndexName_KindexFeedback, timeOut, connectionLimit, IndexType.KindexFeedback);
        }

        static string dataSourceIndexNamePrefix = "data_";
        public static ElasticClient GetESClient(string indexName, int timeOut = 60000, int connectionLimit = 80, IndexType? idxType = null, bool init = true)
        {
            lock (_clientLock)
            {
                if (idxType == IndexType.DataSource)
                    indexName = dataSourceIndexNamePrefix + indexName;
                else if (indexName == defaultIndexName_Audit)
                {
                    //audit_Year-weekInYear
                    DateTime d = DateTime.Now;
                    indexName = $"{indexName}_{d.Year}-{CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
                }
                string cnnset = string.Format("{0}|{1}|{2}", indexName, timeOut, connectionLimit);
                ConnectionSettings sett = GetElasticSearchConnectionSettings(indexName, timeOut, connectionLimit);

                //if (System.Diagnostics.Debugger.IsAttached)
                //    sett.Proxy(new Uri("http://127.0.0.1:8888"),"","");

                if (!_clients.ContainsKey(cnnset))
                {
                    //if (idxType.HasValue == false)
                    //    idxType = GetIndexTypeForDefaultIndexName(indexName);

                    var _client = new ElasticClient(sett);
                    if (init)
                        InitElasticSearchIndex(_client, idxType);

                    _clients.Add(cnnset, _client);
                }
                return _clients[cnnset];
            }

        }


        public static ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000, int? connectionLimit = null)
        {

            string esUrl = Devmasters.Config.GetWebConfigValue("ESConnection");

            //var singlePool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(esUrl));
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
                        ESTraceLogger.Debug($"{call.HttpMethod}\t{call.Uri}\t" +
                            $"{Encoding.UTF8.GetString(call.RequestBodyInBytes)}");
                    }
                    else
                    {
                        ESTraceLogger.Debug($"{call.HttpMethod}\t{call.Uri}\t");
                    }

                })
                ;

            if (System.Diagnostics.Debugger.IsAttached || ESTraceLoggerExists || Devmasters.Config.GetWebConfigValue("ESDebugDataEnabled") == "true")
                settings = settings.DisableDirectStreaming();

            if (connectionLimit.HasValue)
                settings = settings.ConnectionLimit(connectionLimit.Value);

            //.ConnectionLimit(connectionLimit)
            //.MaximumRetries(2)

            //.SetProxy(new Uri("http://localhost.fiddler:8888"), "", "")


#if DEBUG
            //settings = settings.;
#endif
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
                    else
                        goto compareValues;
                }

                compareValues:
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



        //public static void CreateIndex()
        //{
        //    CreateIndex(defaultIndexName);
        //}

        public static void CreateIndex(ElasticClient client)
        {
            IndexSettings set = new IndexSettings();
            set.NumberOfReplicas = 2;
            set.NumberOfShards = 25;

            // Add the Analyzer with a name
            set.Analysis = new Nest.Analysis()
            {
                Analyzers = new Analyzers(),
                TokenFilters = BasicTokenFilters(),
            };

            set.Analysis.Analyzers.Add("default", DefaultAnalyzer());

            IndexState idxSt = new IndexState();
            idxSt.Settings = set;

            var res = client.Indices
                .Create(client.ConnectionSettings.DefaultIndex, i => i
                    .InitializeUsing(idxSt)
                    .Map(mm => mm
                    .Properties(ps => ps
                        .Date(psn => psn.Name("DbCreated"))
                        .Keyword(psn => psn.Name("DbCreatedBy"))
                        )
                    )

                );

        }

        public static void CreateIndex(ElasticClient client, IndexType idxTyp)
        {
            IndexSettings set = new IndexSettings();
            set.NumberOfReplicas = 2;
            if (idxTyp == IndexType.DataSource)
                set.NumberOfShards = 4;
            else if (idxTyp == IndexType.UptimeItem || idxTyp == IndexType.UptimeSSL)
                set.NumberOfShards = 3;
            else 
                set.NumberOfShards = 8;

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
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.VZ.VerejnaZakazka>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.ProfilZadavatele:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.VZ.ProfilZadavatele>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.Insolvence:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.Insolvence.Rizeni>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.Dotace:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.Dotace.Dotace>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.UptimeSSL:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.UptimeSSL>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.UptimeItem:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.UptimeItem>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.Osoby:
                    res = client.Indices
                        .Create(client.ConnectionSettings.DefaultIndex, i => i
                            .InitializeUsing(new IndexState()
                            {
                                Settings = new IndexSettings()
                                {
                                    NumberOfReplicas = 2,
                                    NumberOfShards = 2,
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
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.Smlouva>(map => map.AutoMap().DateDetection(false))
                       );
                    break;
                case IndexType.Firmy:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.FirmaInElastic>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.KIndex:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<KIndexData>(map => map.AutoMap(maxRecursion: 2))
                       );
                    break;
                case IndexType.KIndexTemp:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<KIndexData>(map => map.AutoMap(maxRecursion: 2))
                       );
                    break;
                case IndexType.KIndexBackup:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Backup>(map => map.AutoMap(maxRecursion: 2))
                       );
                    break;
                case IndexType.KIndexBackupTemp:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Backup>(map => map.AutoMap(maxRecursion: 2))
                       );
                    break;
                case IndexType.KindexFeedback:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<KindexFeedback>(map => map.AutoMap(maxRecursion: 2))
                       );
                    break;
                case IndexType.Logs:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(idxSt)
                           .Map<Entities.Logs.ProfilZadavateleDownload>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.Audit:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(new IndexState()
                           {
                               Settings = new IndexSettings()
                               {
                                   NumberOfReplicas = 1,
                                   NumberOfShards = 2
                               }
                           }
                           )
                           .Map<Entities.Audit>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.VerejneZakazkyNaProfiluRaw:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
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
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(new IndexState()
                           {
                               Settings = new IndexSettings()
                               {
                                   NumberOfReplicas = 2,
                                   NumberOfShards = 2
                               }
                           }
                           )
                           .Map<KategorieOVM>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.RPP_OVM:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(new IndexState()
                           {
                               Settings = new IndexSettings()
                               {
                                   NumberOfReplicas = 2,
                                   NumberOfShards = 2
                               }
                           }
                           )
                           .Map<OVMFull>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.RPP_ISVS:
                    res = client.Indices
                       .Create(client.ConnectionSettings.DefaultIndex, i => i
                           .InitializeUsing(new IndexState()
                           {
                               Settings = new IndexSettings()
                               {
                                   NumberOfReplicas = 2,
                                   NumberOfShards = 2
                               }
                           }
                           )
                           .Map<ISVS>(map => map.AutoMap(maxRecursion: 1))
                       );
                    break;
                case IndexType.InDocTableCells:
                    res = client.Indices
                        .Create(client.ConnectionSettings.DefaultIndex, i => i
                            .InitializeUsing(idxSt)
                            .Map<Entities.InDocTableCells>(map => map.AutoMap().DateDetection(false))
                        );
                    break;
            }

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
            ESLogger.Error(new Devmasters.Log.LogMessage()
                    .SetMessage("ES query error: " + text
                        + "\n\nCause:" + serverErr?.Error?.ToString()
                        + "\n\nDetail:" + esReq.DebugInformation
                        + "\n\n\n"
                        )
                    .SetException(ex)
                    .SetCustomKeyValue("URL", httpContext?.Request?.GetDisplayUrl())
                    .SetCustomKeyValue("Stack-trace", Environment.StackTrace)
                    .SetCustomKeyValue("Referer", httpContext?.Request?.GetTypedHeaders()?.Referer?.ToString())
                    .SetCustomKeyValue("User-agent", browser.ToString())
                    //následující řádek byl zkrácen pokud je hostname chtěný, je potřeba volat ještě něco takového: System.Net.Dns.GetHostEntry("127.0.0.1").HostName 
                    .SetCustomKeyValue("IP", HlidacStatu.Util.RealIpAddress.GetIp(httpContext)?.ToString()) //+ " " + System.Web.HttpContext.Current?.Request?.UserHostName  
                    );

        }

        public static Dictionary<string, ElasticClient> GetConnectionPool()
        {
            return _clients;
        }

    }
}
