using Elasticsearch.Net;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Repositories;
using HlidacStatu.Util;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Repositories.Cache;
using Serilog;

namespace HlidacStatu.Datasets
{
    public partial class DataSet
        : IBookmarkable
    {
        private static readonly ILogger _logger = Log.ForContext<DataSet>();
        
        public static DataSet GetCachedDataset(string datasetId) => 
            DataSetCache.Cache.GetOrSet($"_Datasets:{datasetId}", new DataSet(datasetId));
        public static void DeleteCachedDataset(string datasetId) => 
            DataSetCache.Cache.Remove($"_Datasets:{datasetId}");


        public static JsonSerializerSettings DefaultDeserializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static async Task<DataSet> RegisterNewAsync(Registration reg, string user)
        {
            if (reg == null)
                throw new DataSetException(reg.datasetId, ApiResponseStatus.DatasetNotFound);

            if (reg.jsonSchema == null)
                throw new DataSetException(reg.datasetId, ApiResponseStatus.DatasetJsonSchemaMissing);


            reg.NormalizeShortName();
            var client = Manager.GetESClient(reg.datasetId, idxType: Manager.IndexType.DataSource);

            if (reg.searchResultTemplate != null && !string.IsNullOrEmpty(reg.searchResultTemplate?.body))
            {
                var errors = reg.searchResultTemplate.GetTemplateErrors();
                if (errors.Count > 0)
                {
                    var err = ApiResponseStatus.DatasetSearchTemplateError;
                    err.error.errorDetail = string.Join("\n", errors);
                    throw new DataSetException(reg.datasetId, err);
                }
            }

            if (reg.detailTemplate != null && !string.IsNullOrEmpty(reg.detailTemplate?.body))
            {
                var errors = reg.detailTemplate.GetTemplateErrors();
                if (errors.Count > 0)
                {
                    var err = ApiResponseStatus.DatasetDetailTemplateError;
                    err.error.errorDetail = string.Join("\n", errors);
                    throw new DataSetException(reg.datasetId, err);
                }
            }

            var ret = await client.Indices.ExistsAsync(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists)
            {
                throw new DataSetException(reg.datasetId, ApiResponseStatus.DatasetRegistered);
            }
            else
            {
                await Manager.CreateIndexAsync(client);
                await DataSetDB.Instance.AddDataAsync(reg, user);
            }


            return await CreateDataSetInstanceAsync(reg.datasetId, true);
        }

        private async Task<IEnumerable<string>> _getPreviewTopValueFromItemAsync(JObject item,
            bool fromAllTopValues = false)
        {
            List<string> topTxts = new List<string>();

            var props = await GetMappingListAsync("ICO");
            foreach (var prop in props)
            {
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";

                if (DataValidators.CheckCZICO(t))
                {
                    Firma f = Firmy.Get(t);
                    if (f?.Valid == true)
                    {
                        topTxts.Add(f.JmenoBezKoncovky() + ":");
                        if (!fromAllTopValues)
                            break;
                    }
                }
            }

            props = await GetMappingListAsync("Osobaid");
            foreach (var prop in props)
            {
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";

                if (!string.IsNullOrEmpty(t))
                {
                    Osoba os = await OsobaCache.GetPersonByNameIdAsync(t);
                    if (os != null)
                    {
                        topTxts.Add(os.FullName() + ": ");
                        if (!fromAllTopValues)
                            break;
                    }
                }
            }

            return topTxts;
        }

        public async Task<IEnumerable<string>> _getPreviewTextValueFromItemAsync(JObject item)
        {
            List<string> texts = new List<string>();
            var topProps = (await GetMappingListAsync("ICO"))
                .Union(await GetMappingListAsync("Osobaid"));

            var textProps = (await GetTextMappingListAsync()).Except(topProps);
            foreach (var prop in textProps)
            {
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";

                if (!Uri.TryCreate(t, UriKind.Absolute, out Uri tmp))
                {
                    texts.Add(t);
                }
            }

            return texts.OrderByDescending(o => o.Length);
        }

        public async Task<string> GetPreviewTextValueFromItemAsync(JObject item, int maxLength = 320,
            bool useSpecProperties = true,
            bool useTextProperties = true)
        {
            var txts = new List<string>();
            if (useSpecProperties)
                txts.AddRange(await _getPreviewTopValueFromItemAsync(item));
            if (useTextProperties)
                txts.AddRange(await _getPreviewTextValueFromItemAsync(item));

            return Devmasters.TextUtil.ShortenText(
                string.Join(" ", txts)
                , maxLength
            );
        }

        protected ElasticClient client = null;
        protected JSchema schema = null;

        protected string datasetId = null;

        private DataSet(string datasourceName)
        {
            datasetId = datasourceName.ToLower();
            client = Manager.GetESClient(datasetId, idxType: Manager.IndexType.DataSource);
        }

        protected DataSet()
        {
            
        }

        private async Task DataSetAfterInitCheckAsync(bool fireException)
        {
            var ret = await client.Indices.ExistsAsync(client.ConnectionSettings.DefaultIndex);

            if (ret.Exists == false)
            {
                if (fireException)
                    throw new DataSetException(datasetId, ApiResponseStatus.DatasetNotFound);
                else
                    client = null;
            }
        }

        internal static async Task<DataSet> CreateDataSetInstanceAsync(string datasourceName, bool fireException)
        {
            var dataset = new DataSet(datasourceName);
            await dataset.DataSetAfterInitCheckAsync(fireException);
            return dataset;
        }

        public ElasticClient ESClient
        {
            get { return client; }
        }

        IEnumerable<CorePropertyBase> _mapping = null;

        protected async Task<IEnumerable<CorePropertyBase>> GetElasticMappingAsync()
        {
            if (_mapping == null)
            {
                var getIndexResponse = await client.Indices.GetAsync(client.ConnectionSettings.DefaultIndex);
                IIndexState remote = getIndexResponse.Indices.Count > 0
                    ? getIndexResponse.Indices.FirstOrDefault().Value
                    : null;
                var dataMapping = remote?.Mappings?.Properties;
                if (dataMapping == null)
                    return new CorePropertyBase[] { };
                _mapping = dataMapping.Select(m => (CorePropertyBase)m.Value);
            }

            return _mapping;
        }

        public async Task<bool> HasAdminAccessAsync(ClaimsPrincipal user)
        {
            if (user is null)
                return false;

            if (user.IsInRole("Admin"))
                return true;

            return (await RegistrationAsync())?.HasAdminAccess(user) ?? false;
        }

        public virtual Task<DataSearchResult> SearchDataAsync(string queryString, int page, int pageSize,
            string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
            => DatasetRepo.Searching.SearchDataAsync(this, queryString, page, pageSize, sort, excludeBigProperties,
                withHighlighting, exactNumOfResults);

        public virtual Task<DataSearchRawResult> SearchDataRawAsync(string queryString, int page, int pageSize,
            string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
            => DatasetRepo.Searching.SearchDataRawAsync(this, queryString, page, pageSize, sort, excludeBigProperties,
                withHighlighting, exactNumOfResults);

        public async Task<IEnumerable<string>> GetMappingListAsync(string specificMapName = null,
            string attrNameModif = "")
        {
            List<string> properties = new List<string>();
            var mapping = await GetElasticMappingAsync();
            properties.AddRange(GetMappingType("", null, mapping, specificMapName, attrNameModif));
            return properties;
        }

        public async Task<IEnumerable<string>> GetTextMappingListAsync()
        {
            List<string> properties = new List<string>();
            var mapping = await GetElasticMappingAsync();
            properties.AddRange(GetMappingType("", typeof(TextProperty), mapping));
            return properties;
        }

        public async Task<IEnumerable<string>> GetDatetimeMappingListAsync()
        {
            List<string> properties = new List<string>();
            var mapping = await GetElasticMappingAsync();
            properties.AddRange(GetMappingType("", typeof(DateProperty), mapping));
            return properties;
        }

        public async Task<bool> IsPropertyDatetimeAsync(string property)
        {
            var props = await GetMappingListAsync(property);
            foreach (var p in props)
            {
                if (await IsMappedPropertyDatetimeAsync(p) == false)
                    return false;
            }

            return true;
        }

        public async Task<bool> IsMappedPropertyDatetimeAsync(string mappedProperty)
            => (await GetDatetimeMappingListAsync()).Contains(mappedProperty);

        protected IEnumerable<string> GetMappingType(string prefix, Type mappingType,
            IEnumerable<CorePropertyBase> props, string specName = null, string attrNameModif = "")
        {
            List<string> mappingTypes = new List<string>();

            foreach (var p in props)
            {
                if (mappingType == null || p.GetType() == mappingType)
                {
                    if (specName == null || p.Name.Name.ToLower() == specName.ToLower())
                        mappingTypes.Add(prefix + attrNameModif + p.Name.Name + attrNameModif);
                }

                if (p.GetType() == typeof(ObjectProperty))
                {
                    ObjectProperty pObj = (ObjectProperty)p;
                    if (pObj.Properties != null)
                    {
                        mappingTypes.AddRange(GetMappingType(prefix + p.Name.Name + ".", mappingType,
                            pObj.Properties.Select(m => (CorePropertyBase)m.Value), specName, attrNameModif));
                    }
                }
            }

            return mappingTypes;
        }

        public Task<ExpandoObject> ExportFlatObjectAsync(string serializedObj)
        {
            return ExportFlatObjectAsync(
                JsonConvert.DeserializeObject<ExpandoObject>(serializedObj, new ExpandoObjectConverter()));
        }

        public async Task<ExpandoObject> ExportFlatObjectAsync(ExpandoObject obj)
        {
            if (await IsFlatStructureAsync() == false)
                return null;

            IDictionary<String, Object> eo = (IDictionary<String, Object>)obj;
            string[] props = await GetPropertyNamesFromSchemaAsync();
            var toremove = eo.Keys.Where(m => props.Contains(m, StringComparer.InvariantCultureIgnoreCase) == false)
                .ToArray();
            for (int i = 0; i < toremove.Count(); i++)
            {
                ((IDictionary<String, Object>)obj).Remove(toremove[i]);
            }

            return obj;
        }

        public async Task<bool> IsFlatStructureAsync()
        {
            //important from import data from CSV
            var props = await GetPropertyNamesFromSchemaAsync();
            return props.Any(p => p.Contains(".")) == false; //. is delimiter for inner objects
        }

        public async Task<string[]> GetPropertyNameFromSchemaAsync(string name)
        {
            Dictionary<string, Property> names = new Dictionary<string, Property>();
            var sch = await GetSchemaAsync();
            GetPropertyNameTypeFromSchemaInternal(new JSchema[] { sch }, "", name, ref names);
            return names.Keys.ToArray();
        }

        public async Task<string[]> GetPropertyNamesFromSchemaAsync()
        {
            return await GetPropertyNameFromSchemaAsync("");
        }

        public static Dictionary<string, Property> DefaultDatasetProperties = new Dictionary<string, Property>()
        {
            { "hidden", new Property() { Name = "hidden", Type = typeof(bool), Description = "" } },
            {
                "DbCreated",
                new Property() { Name = "DbCreated", Type = typeof(DateTime), Description = "Datum vytvoření záznamu" }
            },
            {
                "DbCreatedBy",
                new Property()
                    { Name = "DbCreatedBy", Type = typeof(string), Description = "Uživatel, který záznam vytvořil" }
            },
        };

        public async Task<Dictionary<string, Property>> GetPropertyNamesTypesFromSchemaAsync(bool addDefaultDatasetProperties = false)
        {
            var properties = await GetPropertyNameTypeFromSchemaAsync("");
            if (addDefaultDatasetProperties)
            {
                foreach (var pp in DefaultDatasetProperties)
                    if (!properties.ContainsKey(pp.Key))
                        properties.Add(pp.Key, pp.Value);
            }

            return properties;
        }

        public async Task<Dictionary<string, Property>> GetPropertyNameTypeFromSchemaAsync(string name)
        {
            Dictionary<string, Property> names = new Dictionary<string, Property>();
            var sch = await GetSchemaAsync();
            GetPropertyNameTypeFromSchemaInternal(new JSchema[] { sch }, "", name, ref names);
            return names;
        }

        private void GetPropertyNameTypeFromSchemaInternal(IEnumerable<JSchema> subschema, string prefix, string name,
            ref Dictionary<string, Property> names)
        {
            foreach (var ss in subschema)
            {
                foreach (var prop in ss.Properties)
                {
                    if (string.IsNullOrEmpty(name)
                        || (!string.IsNullOrEmpty(name) && prop.Key == name)
                       )
                    {
                        names.Add(prefix + prop.Key, JSchemaType2Type(prop.Value));
                    }

                    if (prop.Value.Items.Count > 0)
                        GetPropertyNameTypeFromSchemaInternal(prop.Value.Items, prefix + prop.Key + ".", name,
                            ref names);
                    else if (prop.Value.Properties?.Count > 0)
                    {
                        GetPropertyNameTypeFromSchemaInternal(
                            new JSchema[] { prop.Value }
                            , prefix + prop.Key + ".", name, ref names);
                    }
                }
            }
        }

        private Property JSchemaType2Type(JSchema schema)
        {
            Property ret = new Property();
            if (schema?.Type == null)
                return ret;

            //ret.Name = schema.
            ret.Description = schema.Description;
            JSchemaType s = schema.Type.Value;

            if (Helper.IsSet(s, JSchemaType.Null))
            {
                //nullable types
                if (Helper.IsSet(s, JSchemaType.String))
                {
                    if (schema.Format == "date-time")
                        ret.Type = typeof(Nullable<DateTime>);
                    else if (schema.Format == "date")
                        ret.Type = typeof(Nullable<DateOnly>);
                    else
                        ret.Type = typeof(string);
                }
                else if (Helper.IsSet(s, JSchemaType.Number))
                    ret.Type = typeof(Nullable<decimal>);
                else if (Helper.IsSet(s, JSchemaType.Integer))
                    ret.Type = typeof(Nullable<long>);
                else if (Helper.IsSet(s, JSchemaType.Boolean))
                    ret.Type = typeof(Nullable<bool>);
                else if (Helper.IsSet(s, JSchemaType.Object))
                    ret.Type = typeof(object);
                else if (Helper.IsSet(s, JSchemaType.Array))
                    ret.Type = typeof(object[]);
                else if (Helper.IsSet(s, JSchemaType.None))
                    ret.Type = null;
                else
                    ret.Type = null;
            }
            else
            {
                if (Helper.IsSet(s, JSchemaType.String))
                {
                    if (schema.Format == "date" || schema.Format == "date-time")
                        ret.Type = typeof(DateTime);
                    else if (schema.Format == "date")
                        ret.Type = typeof(DateOnly);
                    else
                        ret.Type = typeof(string);
                }
                else if (Helper.IsSet(s, JSchemaType.Number))
                    ret.Type = typeof(decimal);
                else if (Helper.IsSet(s, JSchemaType.Integer))
                    ret.Type = typeof(long);
                else if (Helper.IsSet(s, JSchemaType.Boolean))
                    ret.Type = typeof(bool);
                else if (Helper.IsSet(s, JSchemaType.Object))
                    ret.Type = typeof(object);
                else if (Helper.IsSet(s, JSchemaType.Array))
                    ret.Type = typeof(object[]);
                else if (Helper.IsSet(s, JSchemaType.None))
                    ret.Type = null;
                else
                    ret.Type = null;
            }

            return ret;
        }

        public async Task SendErrorMsgToAuthorAsync(string url, string errMsg)
        {
            if (Devmasters.TextUtil.IsValidEmail((await RegistrationAsync()).createdBy ?? ""))
            {
                try
                {
                    using (MailMessage msg = new MailMessage("podpora@hlidacstatu.cz",
                               (await RegistrationAsync()).createdBy))
                    {
                        msg.Bcc.Add("michal@michalblaha.cz");
                        msg.Subject = "Chyba v template vasi databáze " + (await RegistrationAsync()).name;
                        msg.IsBodyHtml = false;
                        msg.Body =
                            $"Upozornění!V template vaší databáze {(await RegistrationAsync()).datasetId} na adrese {url} došlo k chybě:\n\n{errMsg}\n\nProsíme opravte ji co nejdříve.\nDíky\n\nTeam Hlídače státu.";
                        msg.BodyEncoding = System.Text.Encoding.UTF8;
                        msg.SubjectEncoding = System.Text.Encoding.UTF8;
                        using (SmtpClient smtp = new SmtpClient())
                        {
                            smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                            _logger.Information("Sending email to " + msg.To);
                            smtp.Send(msg);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Send email");
#if DEBUG
                    throw;
#endif
                }
            }
        }

        private Registration _registration = null;

        public async Task<Registration> RegistrationAsync()
        {
            if (_registration == null)
                _registration = await DataSetDB.Instance.GetRegistrationAsync(datasetId);

            return _registration;
        }

        public string DatasetUrl(bool local = true)
        {
            var url = $"/data/Index/{DatasetId}";
            if (local)
                return url;
            else
                return "https://www.hlidacstatu.cz" + url;
        }

        public string DatasetSearchUrl(string query, bool local = true)
        {
            var url = $"/data/Hledat/{DatasetId}?q={System.Net.WebUtility.UrlEncode(query)}";
            if (local)
                return url;
            else
                return "https://www.hlidacstatu.cz" + url;
        }

        public string DatasetItemUrl(string dataId, bool local = true)
        {
            if (string.IsNullOrEmpty(dataId))
                return string.Empty;

            var url = $"/data/Detail/{DatasetId}/{dataId}";
            if (local)
                return url;
            else
                return "https://www.hlidacstatu.cz" + url;
        }
        

        public string DatasetId
        {
            get { return datasetId; }
        }


        protected async Task<JSchema> GetSchemaAsync()
        {
            if (schema == null)
            {
                schema = (await DataSetDB.Instance.GetRegistrationAsync(DatasetId))?.GetSchema();
            }

            return schema;
        
        }

        public virtual Task<string> AddDataAsync(object data, string id, string createdBy, bool validateSchema = true,
            bool skipallowWriteAccess = false)
            => AddDataAsync(JsonConvert.SerializeObject(data), id, createdBy, validateSchema,
                skipallowWriteAccess: skipallowWriteAccess);

        public virtual async Task<string> AddDataAsync(string data, string id, string createdBy,
            bool validateSchema = true,
            bool skipOCR = false, bool skipallowWriteAccess = false)
        {
            if ((await RegistrationAsync()).allowWriteAccess == false && createdBy != "michal@michalblaha.cz" &&
                skipallowWriteAccess == false)
            {
                if ((await RegistrationAsync()).createdBy != createdBy)
                    throw new DataSetException(datasetId, ApiResponseStatus.DatasetNoPermision);
            }

            JObject obj = JObject.Parse(data);
            dynamic objDyn = JObject.Parse(data);

            var jpaths = obj
                .SelectTokens("$..HsProcessType")
                .ToArray();
            var jpathObjs = jpaths.Select(j => j.Parent.Parent).ToArray();

            if (validateSchema)
            {
                //throws error if schema is not valid
                await CheckSchemaAsync(obj);
            }

            if (string.IsNullOrEmpty(id))
                throw new DataSetException(datasetId, ApiResponseStatus.DatasetItemNoSetID);

            if (objDyn.Id == null
                &&
                objDyn.id == null)
                throw new DataSetException(datasetId, ApiResponseStatus.DatasetItemNoSetID);
            else
                id = objDyn.Id == null ? (string)objDyn.id : (string)objDyn.Id;

            objDyn.DbCreated = DateTime.UtcNow;
            objDyn.DbCreatedBy = createdBy;


            //check special HsProcessType
            if (DatasetId == DataSetDB.DataSourcesDbName) //don't analyze for registration of new dataset
                jpathObjs = new JContainer[] { };

            await FillPersonDataAsync(jpathObjs);

            string updatedData = JsonConvert.SerializeObject(objDyn)
                .Replace((char)160, ' '); //hard space to space
            PostData pd = PostData.String(updatedData);

            var tres = await client.LowLevel.IndexAsync<StringResponse>(client.ConnectionSettings.DefaultIndex, id, pd);

            if (tres.Success)
            {
                JObject jobject = JObject.Parse(tres.Body);

                string finalId = jobject.Value<string>("_id");

                //do DocumentMining after successfull save
                //record must exists before document mining
                if (skipOCR == false)
                {
                    SubscribeToOCR(jpathObjs, finalId);
                }

                SubscribeToAudio(jpathObjs, finalId);

                return finalId;
            }
            else
            {
                var status = ApiResponseStatus.DatasetItemSaveError;
                if (tres.TryGetServerError(out var servererr))
                {
                    status.error.errorDetail = servererr.Error.ToString();
                }

                _logger.Error("AddData id:" + id + "\n" + tres.DebugInformation + "\n" +
                              servererr?.Error?.ToString());

                throw new DataSetException(datasetId, status);
            }
        }

        /// <summary>
        /// Indexing data in bulk
        /// </summary>
        /// <param name="data"></param>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        public async Task<List<string>> AddDataBulkAsync(string data, string createdBy)
        {
            JArray jArray = JArray.Parse(data);

            List<string> bulkRequestBuilder = new List<string>();

            foreach (var jtoken in jArray)
            {
                var jobj = (JObject)jtoken;
                await CheckSchemaAsync(jobj);

                jobj.Add("DbCreated", JToken.FromObject(DateTime.UtcNow));
                jobj.Add("DbCreatedBy", JToken.FromObject(createdBy));

                //check special HsProcessType
                var jpathObjs = new JContainer[] { };

                if (DatasetId != DataSetDB.DataSourcesDbName) //don't analyze for registration of new dataset
                {
                    var jpaths = jobj
                        .SelectTokens("$..HsProcessType")
                        .ToArray();
                    jpathObjs = jpaths.Select(j => j.Parent.Parent).ToArray();
                }

                await FillPersonDataAsync(jpathObjs);

                string id;

                if (jobj["Id"] == null)
                {
                    if (jobj["id"] == null)
                    {
                        throw new DataSetException(datasetId, ApiResponseStatus.DatasetItemNoSetID);
                    }

                    id = jobj["id"].Value<string>();
                }
                else
                {
                    id = jobj["Id"].Value<string>();
                }

                bulkRequestBuilder.Add(
                    $"{{\"index\":{{\"_index\":\"{client.ConnectionSettings.DefaultIndex}\",\"_id\":\"{id}\"}}}}");
                bulkRequestBuilder.Add(jobj.ToString(Formatting.None));
            }

            // Je potřeba sestavit request ručně a použít low-level klienta.
            // NEST nedokáže správně identifikovat klasický dynamic[]. 
            // Při použití expando objektu v dynamic sice jde použít NEST IndexMany, 
            // ale protože expando nemá property, tak nedokáže správně nastavit elastic _id

            PostData pd = PostData.MultiJson(bulkRequestBuilder);
            var result = await client.LowLevel.BulkAsync<BulkResponse>(pd);


            if (!result.IsValid)
            {
                var status = ApiResponseStatus.DatasetItemSaveError;
                status.error.errorDetail =
                    string.Join(";", result.ItemsWithErrors.Select(i => $"{i.Id} - {i.Error.Reason}\n"));
                throw new DataSetException(datasetId, status);
            }

            foreach (var item in result.Items)
            {
                JObject jobj = jArray.Where(jt => (jt["id"]?.Value<string>() == item.Id)
                                                  || (jt["Id"]?.Value<string>() == item.Id))
                    .Select(jt => (JObject)jt)
                    .FirstOrDefault();
                var jpaths = jobj
                    .SelectTokens("$..HsProcessType")
                    .ToArray();
                var jpathObjs = jpaths.Select(j => j.Parent.Parent).ToArray();
                SubscribeToOCR(jpathObjs, item.Id);
            }

            return result.Items.Select(i => i.Id).ToList();
        }


        public static string[] AUDIOCommands = new string[] { "audio", "audiosave" };

        /// <summary>
        /// Register item to Audio Speech2Text queueu
        /// </summary>
        /// <param name="jpathObjs"></param>
        /// <param name="finalId"></param>
        private void SubscribeToAudio(JContainer[] jpathObjs, string finalId)
        {
            int num = 0;
            foreach (var jo in jpathObjs)
            {
                if (AUDIOCommands.Contains(jo["HsProcessType"].Value<string>()))
                {
                    if (jo["AudioUrl"] != null && jo["PrepisAudia"] == null)
                    {
                        if (Uri.TryCreate(jo["AudioUrl"].Value<string>(), UriKind.Absolute, out var uri2audio))
                        {
                            string snum = num == 0 ? "" : "_" + num.ToString();

                            //using (HlidacStatu.Q.Simple.Queue<VoiceDownloadSave> sq = new Q.Simple.Queue<VoiceDownloadSave>(
                            //    Voice2Text.QName,
                            //    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString"))
                            //    )
                            //{
                            //    sq.Send(new VoiceDownloadSave() { dataset = datasetId, itemid = finalId+snum, uri= jo["AudioUrl"].Value<string>() });
                            //}
                            num++;
                        }
                    }
                }
            }
        }


        public static string[] OCRCommands = new string[] { "document", "documentsave" };

        /// <summary>
        /// Register item to OCR query
        /// </summary>
        /// <param name="jpathObjs"></param>
        /// <param name="finalId"></param>
        private void SubscribeToOCR(JContainer[] jpathObjs, string finalId)
        {
            foreach (var jo in jpathObjs)
            {
                if (OCRCommands.Contains(jo["HsProcessType"].Value<string>()))
                {
                    if (jo["DocumentUrl"] != null && string.IsNullOrEmpty(jo["DocumentPlainText"].Value<string>()))
                    {
                        if (Uri.TryCreate(jo["DocumentUrl"].Value<string>(), UriKind.Absolute, out var uri2Ocr))
                        {
                            ItemToOcrQueue.AddNewTask(OcrWork.DocTypes.Dataset,
                                finalId,
                                datasetId,
                                DS.Api.OcrWork.TaskPriority.Standard);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks if Json is valid. If not throws error
        /// </summary>
        private async Task<bool> CheckSchemaAsync(JObject obj)
        {
            JSchema schema = (await DataSetDB.Instance.GetRegistrationAsync(datasetId)).GetSchema();

            if (schema != null)
            {
                IList<string> errors;
                if (!obj.IsValid(schema, out errors))
                {
                    if (errors == null || errors?.Count == 0)
                        errors = new string[] { "", "" };

                    throw DataSetException.GetExc(datasetId,
                        ApiResponseStatus.DatasetItemInvalidFormat.error.number,
                        ApiResponseStatus.DatasetItemInvalidFormat.error.description,
                        string.Join(";", errors)
                    );
                }
            }

            return true;
        }

        public static string[] PERSONLookupCommands = new string[] { "person" };


        /// <summary>
        /// Loop through array and fills in osobaid if array contains any person data
        /// </summary>
        /// <param name="jpathObjs"></param>
        private async Task FillPersonDataAsync(JContainer[] jpathObjs)
        {
            foreach (var jo in jpathObjs)
            {
                if (PERSONLookupCommands.Contains(jo["HsProcessType"].Value<string>()))
                {
                    var jmenoAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "jmeno"
                                    || c.Name.ToLower() == "name")
                        .FirstOrDefault()?.Name;
                    var prijmeniAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "prijmeni"
                                    || c.Name.ToLower() == "surname")
                        .FirstOrDefault()?.Name;
                    var narozeniAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "narozeni"
                                    || c.Name.ToLower() == "birthdate")
                        .FirstOrDefault()?.Name;
                    var osobaIdAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "osobaid")
                        .FirstOrDefault()?.Name ?? "OsobaId";

                    var celejmenoAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "celejmeno"
                                    || c.Name.ToLower() == "fullname")
                        .FirstOrDefault()?.Name;

                    var osobaidAttrName = jo.Children()
                        .Select(c => c as JProperty)
                        .Where(c => c != null)
                        .Where(c => c.Name.ToLower() == "osobaid")
                        .FirstOrDefault()?.Name;
                    if (osobaidAttrName == null)
                        osobaidAttrName = "OsobaId";


                    #region FindOsobaId

                    if (jmenoAttrName != null && prijmeniAttrName != null && narozeniAttrName != null)
                    {
                        if (string.IsNullOrEmpty(jo[osobaidAttrName]?.Value<string>())
                            && jo[narozeniAttrName] != null && jo[narozeniAttrName].Value<DateTime?>().HasValue
                           ) //pokud OsobaId je vyplnena, nehledej jinou
                        {
                            string osobaId = null;
                            var osobaInDb = await OsobaRepo.Searching.GetByNameAsync(
                                jo[jmenoAttrName].Value<string>(),
                                jo[prijmeniAttrName].Value<string>(),
                                jo[narozeniAttrName].Value<DateTime>()
                            );
                            if (osobaInDb == null)
                                osobaInDb = await OsobaRepo.Searching.GetByNameAsciiAsync(
                                    jo[jmenoAttrName].Value<string>(),
                                    jo[prijmeniAttrName].Value<string>(),
                                    jo[narozeniAttrName].Value<DateTime>()
                                );

                            if (osobaInDb != null && string.IsNullOrEmpty(osobaInDb.NameId))
                            {
                                osobaInDb.NameId = await OsobaRepo.GetUniqueNamedIdAsync(osobaInDb);
                                await OsobaRepo.SaveAsync(osobaInDb);
                            }

                            osobaId = osobaInDb?.NameId;
                            jo[osobaidAttrName] = osobaId;
                        }
                    }
                    else if (celejmenoAttrName != null && narozeniAttrName != null)
                    {
                        if (string.IsNullOrEmpty(jo[osobaidAttrName]?.Value<string>())
                            && jo[narozeniAttrName].Value<DateTime?>().HasValue
                           ) //pokud OsobaId je vyplnena, nehledej jinou
                        {
                            string osobaId = null;
                            Osoba osobaZeJmena = Validators.JmenoInText(jo[celejmenoAttrName].Value<string>());
                            if (osobaZeJmena != null)
                            {
                                var osobaInDb = await OsobaRepo.Searching.GetByNameAsync(
                                    osobaZeJmena.Jmeno,
                                    osobaZeJmena.Prijmeni,
                                    jo[narozeniAttrName].Value<DateTime>()
                                );

                                if (osobaInDb == null)
                                    osobaInDb = await OsobaRepo.Searching.GetByNameAsciiAsync(
                                        osobaZeJmena.Jmeno,
                                        osobaZeJmena.Prijmeni,
                                        jo[narozeniAttrName].Value<DateTime>()
                                    );

                                if (osobaInDb != null && string.IsNullOrEmpty(osobaInDb.NameId))
                                {
                                    osobaInDb.NameId = await OsobaRepo.GetUniqueNamedIdAsync(osobaInDb);
                                    await OsobaRepo.SaveAsync(osobaInDb);
                                }

                                osobaId = osobaInDb?.NameId;
                            }

                            jo[osobaidAttrName] = osobaId;
                        }
                    }

                    #endregion
                }
            }
        }

        public async Task<bool> ItemExistsAsync(string Id)
        {
            //GetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, "data", Id);
            var res = await client.LowLevel.DocumentExistsAsync<ExistsResponse>(client.ConnectionSettings.DefaultIndex,
                Id);
            return res.Exists;
        }

        public static List<Uri> GetFromItems_HsDocumentUrls(dynamic dataObj, bool ignoreDocumentPlaintextContent = true,
            int? documentPlaintextContentLengthLess = null)
        {
            if (dataObj == null) return null;
            var jobj = (Newtonsoft.Json.Linq.JObject)dataObj;
            var jpaths = jobj
                .SelectTokens("$..HsProcessType")
                .ToArray();
            var jpathObjs = jpaths.Select(j => j.Parent.Parent).ToArray();

            List<Uri> urisToOcr = new List<Uri>();
            foreach (var jo in jpathObjs)
            {
                if (
                    DataSet.OCRCommands.Contains(jo["HsProcessType"].Value<string>())
                )
                {
                    if (jo["DocumentUrl"] != null
                        && (string.IsNullOrEmpty(jo["DocumentPlainText"].Value<string>())
                            || ignoreDocumentPlaintextContent
                            || (documentPlaintextContentLengthLess.HasValue &&
                                jo["DocumentPlainText"].Value<string>()?.Length <= documentPlaintextContentLengthLess)
                        )
                       )
                    {
                        if (Uri.TryCreate(jo["DocumentUrl"].Value<string>(), UriKind.Absolute, out var uri2Ocr))
                        {
                            //get text from document

                            urisToOcr.Add(uri2Ocr);
                        }
                    }
                }
            }

            return urisToOcr;
        }

        public async Task<dynamic> GetDataObjAsync(string Id)
        {
            var data = await GetDataAsync(Id);
            if (string.IsNullOrEmpty(data))
                return (dynamic)null;
            else
            {
                //var converter = new ExpandoObjectConverter();
                //return JsonConvert.DeserializeObject<ExpandoObject>(data,converter);
                return JObject.Parse(data);
            }
        }

        /// <summary>
        /// Loads data from database.
        /// </summary>
        /// <typeparam name="T">class where data is going to be serialized to</typeparam>
        /// <param name="Id">id field in elastic</param>
        /// <returns>Object T</returns>
        public async Task<T> GetDataAsync<T>(string Id) where T : class
        {
            GetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
            var res = await client.GetAsync<T>(req);
            if (res.Found)
                return res.Source;
            else
                return (T)null;
        }

        public async Task<IEnumerable<dynamic>> GetAllDataForQueryAsync(string queryString, string scrollTimeout = "2m",
            int scrollSize = 1000)
        {
            var fixedQuery = Searching.Tools.FixInvalidQuery(queryString,
                DatasetRepo.Searching.QueryShorcuts,
                DatasetRepo.Searching.QueryOperators);

            QueryContainer qc = await DatasetRepo.Searching.GetSimpleQueryAsync(this, fixedQuery);

            ISearchResponse<dynamic> initialResponse = await client.SearchAsync<dynamic>
            (scr => scr.From(0)
                .Take(scrollSize)
                .Query(q => qc)
                .Scroll(scrollTimeout));

            List<dynamic> results = new List<dynamic>();

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                results.AddRange(initialResponse.Documents);

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<dynamic> loopingResponse = await client.ScrollAsync<dynamic>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            await client.ClearScrollAsync(new ClearScrollRequest(scrollid));

            var expConverter = new ExpandoObjectConverter();

            var data = results
                .Select(m => JsonConvert.SerializeObject(m))
                .Select(s => (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(s, expConverter));

            return data;
        }

        public async Task<IEnumerable<T>> GetAllDataAsync<T>(string scrollTimeout = "2m", int scrollSize = 1000)
            where T : class
        {
            ISearchResponse<dynamic> initialResponse = await client.SearchAsync<dynamic>
            (scr => scr.From(0)
                .Take(scrollSize)
                .MatchAll()
                .Scroll(scrollTimeout));

            List<dynamic> results = new List<dynamic>();

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                results.AddRange(initialResponse.Documents);

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<dynamic> loopingResponse = await client.ScrollAsync<dynamic>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            await client.ClearScrollAsync(new ClearScrollRequest(scrollid));

            var data = results
                .Select(m => JsonConvert.SerializeObject(m))
                .Select(j => JsonConvert.DeserializeObject<T>(j))
                .Cast<T>();

            return data;
        }


        public async Task<string> GetDataAsync(string Id)
        {
            IGetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
            var res = await client.GetAsync<object>(req);
            if (res.Found)
                return JsonConvert.SerializeObject(res.Source);
            else
            {
                req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
                res = await client.GetAsync<object>(req);
                if (res.Found)
                    return JsonConvert.SerializeObject(res.Source);
                else
                    return (string)null;
            }
        }

        public async Task<bool> DeleteDataAsync(string Id)
        {
            var res = await client.LowLevel.DeleteAsync<StringResponse>(client.ConnectionSettings.DefaultIndex, Id);
            return res.Success;
        }

        public static async Task<bool> ExistsDatasetAsync(string datasetId)
        {
            DataSet ds = await CreateDataSetInstanceAsync(datasetId, false);
            return ds.client != null;
        }

        public static string NormalizeValueForId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            id = System.Text.RegularExpressions.Regex.Replace(id, "\\s", "_");
            id = id.Replace("/", "-");
            return id;
        }

        public string GetUrl(bool local = true)
        {
            return GetUrl(local, string.Empty);
        }

        public string GetUrl(bool local, string foundWithQuery)
        {
            string url = "/data/Index/" + DatasetId;
            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

            if (local == false)
                url = "https://www.hlidacstatu.cz" + url;

            return url;
        }

        public string BookmarkName()
        {
#pragma warning disable VSTHRD002
            return RegistrationAsync().GetAwaiter().GetResult().name;
#pragma warning restore VSTHRD002
        }

        public string ToAuditJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "Dataset";
        }

        public string ToAuditObjectId()
        {
            return DatasetId;
        }

        public async Task<string> SocialInfoTitleAsync()
        {
            return Devmasters.TextUtil.ShortenText((await RegistrationAsync()).name, 70);
        }

        public string SocialInfoSubTitle()
        {
            return "";
        }

        public async Task<string> SocialInfoBodyAsync()
        {
            var infoFacts = await InfoFactsAsync();
            return infoFacts.RenderFacts(2, true, html: true);
        }

        public string SocialInfoFooter()
        {
            return "Údaje k " + DateTime.Now.ToString("d. M. yyyy");
        }

        public string SocialInfoImageUrl()
        {
            return "";
        }

        public bool NotInterestingToShow()
        {
            return false;
        }


        //var last = this.SearchData("*", 1, 1, "DbCreated desc");
        long _numberOfRecords = -1;

        public async Task<long> NumberOfRecordsAsync()
        {
            try
            {
                if (_numberOfRecords == -1)
                {
                    var last = await SearchDataAsync("*", 1, 1, "DbCreated desc", exactNumOfResults: true);
                    _numberOfRecords = last.Total;
                    var lRec = last.Result.FirstOrDefault();
                    _lastRecordUpdated = DateTime.Now.AddYears(-10);
                    if (((IDictionary<String, object>)lRec).ContainsKey("DbCreated"))
                    {
                        if (lRec != null)
                            _lastRecordUpdated = lRec?.DbCreated ?? DateTime.Now.AddYears(-10);
                    }
                }

                return _numberOfRecords;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Dataset.NumberOfRecordsAsync");
                return 0;
            }
        }

        DateTime _lastRecordUpdated = DateTime.MinValue;

        public async Task<DateTime> LastRecordUpdatedAsync()
        {
            if (_lastRecordUpdated == DateTime.MinValue)
            {
                var last = await SearchDataAsync("*", 1, 1, "DbCreated desc", exactNumOfResults: true);
                _numberOfRecords = last.Total;
                _lastRecordUpdated = last.Result.FirstOrDefault()?.DbCreated ?? DateTime.Now.AddYears(-10);
            }

            return _lastRecordUpdated;
        }

        InfoFact[] _infofacts = null;

        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<InfoFact[]> InfoFactsAsync()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (_infofacts == null)
                {
                    List<InfoFact> f = new List<InfoFact>();

                    DateTime dbCreated = (await RegistrationAsync()).created;
                    var first = await SearchDataAsync("*", 1, 1, "DbCreated", exactNumOfResults: true);
                    var total = (int)first.Total;
                    var last = await SearchDataAsync("*", 1, 1, "DbCreated desc");
                    if (total == 0)
                    {
                        _infofacts = f.ToArray();
                        return _infofacts;
                    }

                    var itemFirstDate = (DateTime)first.Result.First().DbCreated;
                    var itemLastDate = (DateTime)last.Result.First().DbCreated;

                    dbCreated = new DateTime(Math.Min(dbCreated.Ticks, itemFirstDate.Ticks));
                    var sCreated =
                        $"Databáze byla založena {Devmasters.DT.Util.Ago(dbCreated, Consts.csCulture).ToLower()}. ";
                    string minMax = "";
                    if (total == 0)
                    {
                        minMax += "Neobsahuje žádný záznam";
                    }
                    else
                    {
                        minMax += Devmasters.Lang.CS.Plural.GetWithZero(total, "Neobsahuje žádný záznam",
                                      "Obsahuje <b>jeden záznam</b>", "Obsahuje <b>{0} záznamy</b>",
                                      "Obsahuje <b>{0} záznamů</b>")
                                  + ", nejstarší byl vložen <b>"
                                  + (Devmasters.DT.Util.Ago(itemFirstDate, Consts.csCulture).ToLower())
                                  + "</b>, nejnovější <b>" +
                                  (Devmasters.DT.Util.Ago(itemLastDate, Consts.csCulture).ToLower())
                                  + "</b>.";
                    }

                    var stat = sCreated + " " + minMax;
                    f.Add(new InfoFact(stat, InfoFact.ImportanceLevel.Stat));
                    _infofacts = f.ToArray();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return _infofacts;
        }
    }
}