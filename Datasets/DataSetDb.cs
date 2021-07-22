using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;

namespace HlidacStatu.Datasets
{
    public class DataSetDB : DataSet
    {
        public static string DataSourcesDbName = "datasourcesdb";

        public static DataSetDB Instance = new DataSetDB();


        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<DataSet[]> AllDataSets =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<DataSet[]>(
                        TimeSpan.FromMinutes(5), (obj) =>
                        {

                            var datasets = Instance.SearchDataRaw("*", 1, 500)
                            .Result
                            .Select(s => CachedDatasets.Get(s.Item1))
                            .Where(d => d != null)
                            .ToArray();

                            return datasets;
                        }
                    );

        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<DataSet[]> ProductionDataSets =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<DataSet[]>(
                        TimeSpan.FromMinutes(5), (obj) =>
                        {

                            var datasets = Instance.SearchDataRaw("*", 1, 500)
                            .Result
                            .Select(s => CachedDatasets.Get(s.Item1))
                            .Where(d => d != null)
                            .Where(d => d.Registration().betaversion == false && d.Registration().hidden == false)
                            .ToArray();

                            return datasets;
                        }
                    );

        //    var datasets = HlidacStatu.Connectors.External.DataSets.DataSetDB.Instance.SearchDataRaw("*", 1, 100)
        //.Result
        //.Select(s => Newtonsoft.Json.JsonConvert.DeserializeObject<HlidacStatu.Connectors.External.DataSets.Registration>(s.Item2));


        private DataSetDB() : base(DataSourcesDbName, false)
        {

            if (client == null)
            {
                client = Manager.GetESClient(DataSourcesDbName, idxType: Manager.IndexType.DataSource);
                var ret = client.Indices.Exists(client.ConnectionSettings.DefaultIndex); 
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

                    var tres = client.LowLevel.Index<Elasticsearch.Net.StringResponse>(client.ConnectionSettings.DefaultIndex, DataSourcesDbName, pd);
                    if (tres.Success == false)
                        throw new ApplicationException(tres.DebugInformation);
                }
            }
        }

        public Registration GetRegistration(string datasetId)
        {
            datasetId = datasetId.ToLower();
            var s = GetData(datasetId);
            if (string.IsNullOrEmpty(s))
                return null;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(s, DefaultDeserializationSettings);
        }


        public virtual string AddData(Registration reg, string user,bool skipallowWriteAccess = false)
        {
            if (reg.jsonSchema == null)
                throw new DataSetException(datasetId, ApiResponseStatus.DatasetJsonSchemaMissing);

            Registration oldReg = null;
            oldReg = CachedDatasets.Get(reg.datasetId)?.Registration();
            if (oldReg == null)
                AuditRepo.Add<Registration>(Audit.Operations.Create, user, reg, null);
            else
                AuditRepo.Add<Registration>(Audit.Operations.Update, user, reg, oldReg);

            var addDataResult = base.AddData(reg, reg.datasetId, reg.createdBy, validateSchema:false, skipallowWriteAccess: true);
            CachedDatasets.Delete(reg.datasetId);

            //check orderList
            if (reg.orderList?.Length > 0)
            {

                //get mapping
                var ds = CachedDatasets.Get(addDataResult);
                var txtProps = ds.GetTextMappingList();
                var allProps = ds.GetMappingList();
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
                        addDataResult = base.AddData(reg, reg.datasetId, reg.createdBy);
                }

            }
            CachedDatasets.Set(reg.datasetId, null);

            return addDataResult;
        }


        public bool DeleteRegistration(string datasetId, string user)
        {
            AuditRepo.Add<Registration>(Audit.Operations.Delete, user, Registration(), null);

            datasetId = datasetId.ToLower();
            var res = DeleteData(datasetId);
            var idxClient = Manager.GetESClient(datasetId, idxType: Manager.IndexType.DataSource);

            var delRes = idxClient.Indices.Delete(idxClient.ConnectionSettings.DefaultIndex);
            CachedDatasets.Delete(datasetId);
            return res && delRes.IsValid;
        }


        public override DataSearchResult SearchData(string queryString, int page, int pageSize, string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            var resData = base.SearchData(queryString, page, pageSize, sort, excludeBigProperties, withHighlighting, exactNumOfResults);
            if (resData == null || resData?.Result == null)
                return resData;

            resData.Result = resData.Result.Where(r => r.id.ToString() != DataSourcesDbName);

            return resData;

        }
        public override DataSearchRawResult SearchDataRaw(string queryString, int page, int pageSize, string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            var resData = base.SearchDataRaw($"NOT(id:{DataSourcesDbName}) AND ({queryString})", page, pageSize,
                sort, excludeBigProperties, withHighlighting, exactNumOfResults);
            //var resData = base.SearchDataRaw($"({queryString})", page, pageSize, sort);
            if (resData == null || resData?.Result == null)
                return resData;

            return resData;

        }


    }
}
