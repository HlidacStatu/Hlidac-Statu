﻿using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Newtonsoft.Json;
using Serilog;

namespace HlidacStatu.Datasets
{
    public class DataSetDB : DataSet
    {
        public static string DataSourcesDbName = "datasourcesdb";

        public static DataSetDB Instance = new DataSetDB();
        
        private readonly ILogger _logger = Log.ForContext<DataSetDB>();


        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<DataSet[]> AllDataSets =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<DataSet[]>(
                        TimeSpan.FromMinutes(5), (obj) =>
                        {
                            var datasets = Instance.SearchDataRawAsync("*", 1, 500)
                                .ConfigureAwait(false).GetAwaiter().GetResult()
                            .Result
                            .Select(s => CachedDatasets.Get(s.Item1))
                            .Where(d => d != null)
                            .ToArray();

                            return datasets;
                        }
                    );

        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<DataSet[]> ProductionDataSets =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<DataSet[]>(
                        TimeSpan.FromMinutes(5), (obj) =>
                        {

                            var datasets = Instance.SearchDataRawAsync("*", 1, 500)
                                .ConfigureAwait(false).GetAwaiter().GetResult()
                            .Result
                            .Select(s => CachedDatasets.Get(s.Item1))
                            .Where(d => d != null)
                            .Where(d => d.RegistrationAsync().ConfigureAwait(false).GetAwaiter().GetResult().betaversion == false 
                                        && d.RegistrationAsync().ConfigureAwait(false).GetAwaiter().GetResult().hidden == false)
                            .ToArray();

                            return datasets;
                        }
                    );

        
        private DataSetDB() : base(DataSourcesDbName, false)
        {

            if (client == null)
            {
                client = Manager.GetESClient(DataSourcesDbName, idxType: Manager.IndexType.DataSource);
                    
                var ret = client.Indices.ExistsAsync(client.ConnectionSettings.DefaultIndex)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                if (!ret.Exists)
                {
                    Newtonsoft.Json.Schema.Generation.JSchemaGenerator jsonG = new Newtonsoft.Json.Schema.Generation.JSchemaGenerator();
                    jsonG.DefaultRequired = Newtonsoft.Json.Required.Default;
                    Registration reg = new Registration()
                    {
                        datasetId = DataSourcesDbName,
                        jsonSchema = jsonG.Generate(typeof(Registration)).ToString()
                    };
                    Manager.CreateIndex(client);

                    //add record
                    Elasticsearch.Net.PostData pd = Elasticsearch.Net.PostData.String(Newtonsoft.Json.JsonConvert.SerializeObject(reg));

                    var tres = client.LowLevel.IndexAsync<Elasticsearch.Net.StringResponse>(client.ConnectionSettings.DefaultIndex, DataSourcesDbName, pd)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    if (tres.Success == false)
                        throw new ApplicationException(tres.DebugInformation);
                }
            }
        }

        public async Task<Registration> GetRegistrationAsync(string datasetId)
        {
            datasetId = datasetId.ToLower();
            var s = await GetDataAsync(datasetId);
            if (string.IsNullOrEmpty(s))
                return null;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(s, DefaultDeserializationSettings);
        }


        public virtual async Task<string> AddDataAsync(Registration reg, string user, bool skipallowWriteAccess = false)
        {
            if (reg.jsonSchema == null)
                throw new DataSetException(datasetId, ApiResponseStatus.DatasetJsonSchemaMissing);

            Registration oldReg = null;
            oldReg = await CachedDatasets.Get(reg.datasetId)?.RegistrationAsync();
            if (oldReg == null)
                AuditRepo.Add<Registration>(Audit.Operations.Create, user, reg, null);
            else
                AuditRepo.Add<Registration>(Audit.Operations.Update, user, reg, oldReg);

            var addDataResult = await base.AddDataAsync(reg, reg.datasetId, reg.createdBy, validateSchema: false, skipallowWriteAccess: true);
            CachedDatasets.Delete(reg.datasetId);

            //check orderList
            if (reg.orderList?.Length > 0)
            {

                //get mapping
                var ds = CachedDatasets.Get(addDataResult);
                
                var txtPropsTask = ds.GetTextMappingListAsync();
                var allPropsTask = ds.GetMappingListAsync();
                var txtProps = await txtPropsTask;
                var allProps = await allPropsTask;
                if (allProps.Where(m => !DefaultDatasetProperties.Keys.Contains(m)).Count() > 0) //0=mapping not available , (no record in db)
                {

                    bool changedOrderList = false;

                    //find missing and remove it
                    List<int> toRemove = new List<int>();
                    for (int i = 0; i < reg.orderList.GetLength(0); i++)
                    {

                        string oProp = reg.orderList[i, 1]
                            .Replace(DataSearchResult.OrderAsc, "")
                            .Replace(DataSearchResult.OrderDesc, "")
                            .Replace(DataSearchResult.OrderAscUrl, "")
                            .Replace(DataSearchResult.OrderDescUrl, "")
                            .Trim();
                        if (oProp.EndsWith(".keyword"))
                            oProp = System.Text.RegularExpressions.Regex.Replace(oProp, "\\.keyword$", "");
                        if (allProps.Contains(oProp) == false)
                        {
                            //neni na seznamu properties, pridej do seznamu k smazani
                            toRemove.Add(i);
                        }
                    }
                    if (toRemove.Count > 0)
                    {
                        foreach (var i in toRemove.OrderByDescending(n => n))
                        {
                            reg.orderList = Devmasters.Collections.ArrayExt.TrimArray<string>(i, null, reg.orderList);
                            changedOrderList = true;
                        }
                    }

                    for (int i = 0; i < reg.orderList.GetLength(0); i++)
                    {
                        string oProp = reg.orderList[i, 1]
                            .Replace(DataSearchResult.OrderAsc, "")
                            .Replace(DataSearchResult.OrderAscUrl, "")
                            .Replace(DataSearchResult.OrderDesc, "")
                            .Replace(DataSearchResult.OrderDescUrl, "")
                            .Trim();
                        if (txtProps.Contains(oProp))
                        {
                            //pridej keyword na konec
                            reg.orderList[i, 1] = reg.orderList[i, 1].Replace(oProp, oProp + ".keyword");
                            changedOrderList = true;
                        }
                    }

                    if (changedOrderList)
                        addDataResult = await base.AddDataAsync(reg, reg.datasetId, reg.createdBy);
                }

            }
            CachedDatasets.Set(reg.datasetId, null);

            return addDataResult;
        }


        public async Task<bool> DeleteRegistrationAsync(string datasetId, string user)
        {
            AuditRepo.Add<Registration>(Audit.Operations.Delete, user, (await RegistrationAsync()), null);

            if (datasetId.ToLower() == "datasourcesdb")
                return true;


            datasetId = datasetId.ToLower();
            var res = await DeleteDataAsync(datasetId);
            var idxClient = Manager.GetESClient(datasetId, idxType: Manager.IndexType.DataSource);

            //delete /hs-data_rozhodnuti-uohs*
            for (int i = 1; i < 99; i++)
            {
                var _dsId = $"hs-data_{datasetId}-{i:00}";
                var delRes = idxClient.Indices.Delete(_dsId);
                if (delRes.IsValid == false)
                    break;

            }

            CachedDatasets.Delete(datasetId);
            return res;
        }

        public override async Task<DataSearchResult> SearchDataAsync(string queryString, int page, int pageSize,
            string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            var resData = await base.SearchDataAsync(queryString, page, pageSize, sort, excludeBigProperties, withHighlighting, exactNumOfResults);
            if (resData == null || resData?.Result == null)
                return resData;

            resData.Result = resData.Result.Where(r => r.id.ToString() != DataSourcesDbName);

            return resData;

        }
        
        public override async Task<DataSearchRawResult> SearchDataRawAsync(string queryString, int page, int pageSize,
            string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            var resData = await base.SearchDataRawAsync($"NOT(id:{DataSourcesDbName}) AND ({queryString})", page, pageSize,
                sort, excludeBigProperties, withHighlighting, exactNumOfResults);
            //var resData = base.SearchDataRaw($"({queryString})", page, pageSize, sort);
            if (resData == null || resData?.Result == null)
                return resData;

            return resData;
        }

        public async Task<List<Registration>> SearchInDatasetsAsync(string query, int page, int pageSize)
        {
            var resData = await base.SearchDataRawAsync($"NOT(id:{DataSourcesDbName}) AND ({query})", page, pageSize);
            if (resData == null || resData?.Result == null)
                return null;

            var results = new List<Registration>();
            
            foreach (var res in resData.Result)
            {
                try
                {
                    results.Add(JsonConvert.DeserializeObject<Registration>(res.Item2, DefaultDeserializationSettings));
                }
                catch (Exception e)
                {
                    _logger.Information(e, $"Current result is not serializable into {nameof(Registration)} class. Query [{query}]");
                }
            }

            return results;

        }
    }
}
