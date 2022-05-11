using Elasticsearch.Net;

using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;
using HlidacStatu.Util.Cache;

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

namespace HlidacStatu.Datasets
{
    public partial class DataSet
        : IBookmarkable
    {
        public static volatile MemoryCacheManager<DataSet, string> CachedDatasets
            = MemoryCacheManager<DataSet, string>.GetSafeInstance("Datasets",
                datasetId => { return new DataSet(datasetId); },
                TimeSpan.FromMinutes(10));


        public static JsonSerializerSettings DefaultDeserializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static DataSet RegisterNew(Registration reg, string user)
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
                    err.error.errorDetail = errors.Aggregate((f, s) => f + "\n" + s);
                    throw new DataSetException(reg.datasetId, err);
                }
            }

            if (reg.detailTemplate != null && !string.IsNullOrEmpty(reg.detailTemplate?.body))
            {
                var errors = reg.detailTemplate.GetTemplateErrors();
                if (errors.Count > 0)
                {
                    var err = ApiResponseStatus.DatasetDetailTemplateError;
                    err.error.errorDetail = errors.Aggregate((f, s) => f + "\n" + s);
                    throw new DataSetException(reg.datasetId, err);
                }
            }

            var ret = client.Indices.Exists(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists)
            {
                throw new DataSetException(reg.datasetId, ApiResponseStatus.DatasetRegistered);
            }
            else
            {
                Manager.CreateIndex(client);
                DataSetDB.Instance.AddData(reg, user);
            }


            return new DataSet(reg.datasetId);
        }

        private IEnumerable<string> _getPreviewTopValueFromItem(JObject item, bool fromAllTopValues = false)
        {
            List<string> topTxts = new List<string>();
            List<string> texts = new List<string>();
            var props = GetMappingList("ICO");
            foreach (var prop in props)
            {
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";
                //var t = ((string)Dynamitey.Dynamic.InvokeGetChain(item, prop)) ?? "";
                if (DataValidators.CheckCZICO(t))
                {
                    Firma f = Firmy.Get(t);
                    if (f.Valid)
                    {
                        topTxts.Add(f.JmenoBezKoncovky() + ":");
                        if (!fromAllTopValues)
                            break;
                    }
                }
            }

            props = GetMappingList("Osobaid");
            foreach (var prop in props)
            {
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";
                //var t = ((string)Dynamitey.Dynamic.InvokeGetChain(item, prop)) ?? "";
                if (!string.IsNullOrEmpty(t))
                {
                    Osoba os = Osoby.GetByNameId.Get(t);
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

        public IEnumerable<string> _getPreviewTextValueFromItem(JObject item)
        {
            List<string> topTxts = new List<string>();
            List<string> texts = new List<string>();
            var topProps = GetMappingList("ICO")
                .Union(GetMappingList("Osobaid"));


            var textProps = GetTextMappingList().Except(topProps);
            foreach (var prop in textProps)
            {
                //var t = ((string)Dynamitey.Dynamic.InvokeGetChain(item, prop)) ?? "";
                var o = item.SelectTokens(prop).FirstOrDefault();
                string t = "";
                if (o != null && o.GetType() == typeof(JValue))
                    t = o.Value<string>() ?? "";

                if (!Uri.TryCreate(t, UriKind.Absolute, out Uri tmp))
                {
                    //migrace: je potřeba při vzniku záznamu naplnit SocialShareText v datasetu
                    texts.Add(t);

                }
            }

            return texts.OrderByDescending(o => o.Length);
        }

        public string GetPreviewTextValueFromItem(JObject item, int maxLength = 320, bool useSpecProperties = true,
            bool useTextProperties = true)
        {
            var txts = new List<string>();
            if (useSpecProperties)
                txts.AddRange(_getPreviewTopValueFromItem(item));
            if (useTextProperties)
                txts.AddRange(_getPreviewTextValueFromItem(item));

            return Devmasters.TextUtil.ShortenText(
                string.Join(" ", txts)
                , maxLength
            );
        }

        protected ElasticClient client = null;
        protected JSchema schema = null;

        protected string datasetId = null;

        protected DataSet(string datasourceName, bool fireException)
        {
            datasetId = datasourceName.ToLower();
            client = Manager.GetESClient(datasetId, idxType: Manager.IndexType.DataSource);


            var ret = client.Indices.Exists(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists == false)
            {
                if (fireException)
                    throw new DataSetException(datasetId, ApiResponseStatus.DatasetNotFound);
                else
                    client = null;
            }
        }

        public ElasticClient ESClient
        {
            get { return client; }
        }

        IEnumerable<CorePropertyBase> _mapping = null;

        protected IEnumerable<CorePropertyBase> GetElasticMapping()
        {
            if (_mapping == null)
            {
                var getIndexResponse = client.Indices.Get(client.ConnectionSettings.DefaultIndex);
                IIndexState remote = getIndexResponse.Indices[client.ConnectionSettings.DefaultIndex];
                var dataMapping = remote?.Mappings?.Properties;
                if (dataMapping == null)
                    return new CorePropertyBase[] { };
                _mapping = dataMapping.Select(m => (CorePropertyBase)m.Value);
            }

            return _mapping;
        }

        public bool HasAdminAccess(ClaimsPrincipal user)
        {
            if (user is null)
                return false;

            if (user.IsInRole("Admin"))
                return true;

            return Registration()?.HasAdminAccess(user) ?? false;
        }

        public virtual DataSearchResult SearchData(string queryString, int page, int pageSize, string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            return DatasetRepo.Searching.SearchData(this, queryString, page, pageSize, sort, excludeBigProperties,
                withHighlighting, exactNumOfResults);
        }

        public virtual DataSearchRawResult SearchDataRaw(string queryString, int page, int pageSize, string sort = null,
            bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
        {
            return DatasetRepo.Searching.SearchDataRaw(this, queryString, page, pageSize, sort, excludeBigProperties,
                withHighlighting, exactNumOfResults);
        }

        public IEnumerable<string> GetMappingList(string specificMapName = null, string attrNameModif = "")
        {
            List<string> properties = new List<string>();
            var mapping = GetElasticMapping();
            properties.AddRange(getMappingType("", null, mapping, specificMapName, attrNameModif));
            return properties;
        }

        public IEnumerable<string> GetTextMappingList()
        {
            List<string> properties = new List<string>();
            var mapping = GetElasticMapping();
            properties.AddRange(getMappingType("", typeof(TextProperty), mapping));
            return properties;
        }

        public IEnumerable<string> GetDatetimeMappingList()
        {
            List<string> properties = new List<string>();
            var mapping = GetElasticMapping();
            properties.AddRange(getMappingType("", typeof(DateProperty), mapping));
            return properties;
        }

        public bool IsPropertyDatetime(string property)
        {
            var props = GetMappingList(property);
            var isDt = true;
            foreach (var p in props)
            {
                isDt = isDt && IsMappedPropertyDatetime(p);
                if (isDt == false)
                    return false;
            }

            return isDt;
        }

        public bool IsMappedPropertyDatetime(string mappedProperty)
        {
            return GetDatetimeMappingList().Contains(mappedProperty);
        }

        protected IEnumerable<string> getMappingType(string prefix, Type mappingType,
            IEnumerable<CorePropertyBase> props, string specName = null, string attrNameModif = "")
        {
            List<string> _props = new List<string>();

            foreach (var p in props)
            {
                if (mappingType == null || p.GetType() == mappingType)
                {
                    if (specName == null || p.Name.Name.ToLower() == specName.ToLower())
                        _props.Add(prefix + attrNameModif + p.Name.Name + attrNameModif);
                }

                if (p.GetType() == typeof(ObjectProperty))
                {
                    ObjectProperty pObj = (ObjectProperty)p;
                    if (pObj.Properties != null)
                    {
                        _props.AddRange(getMappingType(prefix + p.Name.Name + ".", mappingType,
                            pObj.Properties.Select(m => (CorePropertyBase)m.Value), specName, attrNameModif));
                    }
                }
            }

            return _props;
        }

        public ExpandoObject ExportFlatObject(string serializedObj)
        {
            return ExportFlatObject(
                JsonConvert.DeserializeObject<ExpandoObject>(serializedObj, new ExpandoObjectConverter()));
        }

        public ExpandoObject ExportFlatObject(ExpandoObject obj)
        {
            if (IsFlatStructure() == false)
                return null;

            IDictionary<String, Object> eo = (IDictionary<String, Object>)obj;
            var props = GetPropertyNamesFromSchema();
            var toremove = eo.Keys.Where(m => props.Contains(m) == false).ToArray();
            for (int i = 0; i < toremove.Count(); i++)
            {
                ((IDictionary<String, Object>)obj).Remove(toremove[i]);
            }

            return obj;
        }


        public bool IsFlatStructure()
        {
            //important from import data from CSV
            var props = GetPropertyNamesFromSchema();
            return props.Any(p => p.Contains(".")) == false; //. is delimiter for inner objects
        }

        public string[] GetPropertyNameFromSchema(string name)
        {
            Dictionary<string, Property> names = new Dictionary<string, Property>();
            var sch = Schema;
            getPropertyNameTypeFromSchemaInternal(new JSchema[] { sch }, "", name, ref names);
            return names.Keys.ToArray();
        }

        public string[] GetPropertyNamesFromSchema()
        {
            return GetPropertyNameFromSchema("");
        }

        public static Dictionary<string, Property> DefaultDatasetProperties = new Dictionary<string, Property>()
        {
            {"hidden", new Property() {Name = "hidden", Type = typeof(bool), Description = ""}},
            {
                "DbCreated",
                new Property() {Name = "DbCreated", Type = typeof(DateTime), Description = "Datum vytvoření záznamu"}
            },
            {
                "DbCreatedBy",
                new Property()
                    {Name = "DbCreatedBy", Type = typeof(string), Description = "Uživatel, který záznam vytvořil"}
            },
        };

        public Dictionary<string, Property> GetPropertyNamesTypesFromSchema(bool addDefaultDatasetProperties = false)
        {
            var properties = GetPropertyNameTypeFromSchema("");
            if (addDefaultDatasetProperties)
            {
                foreach (var pp in DefaultDatasetProperties)
                    if (!properties.ContainsKey(pp.Key))
                        properties.Add(pp.Key, pp.Value);
            }

            return properties;
        }

        public Dictionary<string, Property> GetPropertyNameTypeFromSchema(string name)
        {
            Dictionary<string, Property> names = new Dictionary<string, Property>();
            var sch = Schema;
            getPropertyNameTypeFromSchemaInternal(new JSchema[] { sch }, "", name, ref names);
            return names;
        }

        private void getPropertyNameTypeFromSchemaInternal(IEnumerable<JSchema> subschema, string prefix, string name,
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
                        getPropertyNameTypeFromSchemaInternal(prop.Value.Items, prefix + prop.Key + ".", name,
                            ref names);
                    else if (prop.Value.Properties?.Count > 0)
                    {
                        getPropertyNameTypeFromSchemaInternal(
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
                        ret.Type = typeof(Nullable<Devmasters.DT.Date>);
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
                        ret.Type = typeof(Devmasters.DT.Date);
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

        public void SendErrorMsgToAuthor(string url, string errMsg)
        {
            if (Devmasters.TextUtil.IsValidEmail(Registration().createdBy ?? ""))
            {
                try
                {
                    using (MailMessage msg = new MailMessage("podpora@hlidacstatu.cz", Registration().createdBy))
                    {
                        msg.Bcc.Add("michal@michalblaha.cz");
                        msg.Subject = "Chyba v template vasi databáze " + Registration().name;
                        msg.IsBodyHtml = false;
                        msg.Body =
                            $"Upozornění!V template vaší databáze {Registration().datasetId} na adrese {url} došlo k chybě:\n\n{errMsg}\n\nProsíme opravte ji co nejdříve.\nDíky\n\nTeam Hlídače státu.";
                        msg.BodyEncoding = System.Text.Encoding.UTF8;
                        msg.SubjectEncoding = System.Text.Encoding.UTF8;
                        using (SmtpClient smtp = new SmtpClient())
                        {
                            smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                            Consts.Logger.Info("Sending email to " + msg.To);
                            smtp.Send(msg);
                        }
                    }
                }
                catch (Exception e)
                {
                    Consts.Logger.Error("Send email", e);
#if DEBUG
                    throw;
#endif
                }
            }
        }

        private Registration _registration = null;

        public Registration Registration()
        {
            if (_registration == null)
                _registration = DataSetDB.Instance.GetRegistration(datasetId);

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

        protected DataSet(string datasourceName) : this(datasourceName, true)
        {
        }

        public string DatasetId
        {
            get { return datasetId; }
        }


        protected JSchema Schema
        {
            get
            {
                if (schema == null)
                {
                    schema = DataSetDB.Instance.GetRegistration(DatasetId)
                        ?.GetSchema();
                }

                return schema;
            }
        }

        public virtual string AddData(object data, string id, string createdBy, bool validateSchema = true,
            bool skipallowWriteAccess = false)
        {
            return AddData(JsonConvert.SerializeObject(data), id, createdBy, validateSchema,
                skipallowWriteAccess: skipallowWriteAccess);
        }

        public virtual string AddData(string data, string id, string createdBy, bool validateSchema = true,
            bool skipOCR = false, bool skipallowWriteAccess = false)
        {
            if (Registration().allowWriteAccess == false && createdBy != "michal@michalblaha.cz" &&
                skipallowWriteAccess == false)
            {
                if (Registration().createdBy != createdBy)
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
                CheckSchema(obj);
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

            FillPersonData(jpathObjs);

            string updatedData = JsonConvert.SerializeObject(objDyn)
                .Replace((char)160, ' '); //hard space to space
            PostData pd = PostData.String(updatedData);

            var tres = client.LowLevel.Index<StringResponse>(client.ConnectionSettings.DefaultIndex, id, pd);

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

                Consts.Logger.Error("AddData id:" + id + "\n" + tres.DebugInformation + "\n" +
                                    servererr?.Error?.ToString());

                throw new DataSetException(datasetId, status);
            }

            //return res.ToString();
            //ElasticsearchResponse<string> result = this.client.Raw.Index(document.Index, document.Type, document.Id, documentJson);
        }

        /// <summary>
        /// Indexing data in bulk
        /// </summary>
        /// <param name="data"></param>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        public List<string> AddDataBulk(string data, string createdBy)
        {
            JArray jArray = JArray.Parse(data);

            List<string> bulkRequestBuilder = new List<string>();

            foreach (var jtoken in jArray)
            {
                var jobj = (JObject)jtoken;
                CheckSchema(jobj);

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

                FillPersonData(jpathObjs);

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
            var result = client.LowLevel.Bulk<BulkResponse>(pd);


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
                            ItemToOcrQueue.AddNewTask(ItemToOcrQueue.ItemToOcrType.Dataset,
                                finalId,
                                datasetId,
                                Lib.OCR.Api.Client.TaskPriority.Standard);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks if Json is valid. If not throws error
        /// </summary>
        private bool CheckSchema(JObject obj)
        {
            JSchema schema = DataSetDB.Instance.GetRegistration(datasetId).GetSchema();

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
                        errors.Aggregate((f, s) => f + ";" + s)
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
        private void FillPersonData(JContainer[] jpathObjs)
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
                            var osobaInDb = OsobaRepo.Searching.GetByName(
                                jo[jmenoAttrName].Value<string>(),
                                jo[prijmeniAttrName].Value<string>(),
                                jo[narozeniAttrName].Value<DateTime>()
                            );
                            if (osobaInDb == null)
                                osobaInDb = OsobaRepo.Searching.GetByNameAscii(
                                    jo[jmenoAttrName].Value<string>(),
                                    jo[prijmeniAttrName].Value<string>(),
                                    jo[narozeniAttrName].Value<DateTime>()
                                );

                            if (osobaInDb != null && string.IsNullOrEmpty(osobaInDb.NameId))
                            {
                                osobaInDb.NameId = OsobaRepo.GetUniqueNamedId(osobaInDb);
                                OsobaRepo.Save(osobaInDb);
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
                                var osobaInDb = OsobaRepo.Searching.GetByName(
                                    osobaZeJmena.Jmeno,
                                    osobaZeJmena.Prijmeni,
                                    jo[narozeniAttrName].Value<DateTime>()
                                );

                                if (osobaInDb == null)
                                    osobaInDb = OsobaRepo.Searching.GetByNameAscii(
                                        osobaZeJmena.Jmeno,
                                        osobaZeJmena.Prijmeni,
                                        jo[narozeniAttrName].Value<DateTime>()
                                    );

                                if (osobaInDb != null && string.IsNullOrEmpty(osobaInDb.NameId))
                                {
                                    osobaInDb.NameId = OsobaRepo.GetUniqueNamedId(osobaInDb);
                                    OsobaRepo.Save(osobaInDb);
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

        public bool ItemExists(string Id)
        {
            //GetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, "data", Id);
            var res = client.LowLevel.DocumentExists<ExistsResponse>(client.ConnectionSettings.DefaultIndex, Id);
            return res.Exists;
        }

        public dynamic GetDataObj(string Id)
        {
            var data = GetData(Id);
            if (string.IsNullOrEmpty(data))
                return (dynamic)null;
            else
                return JObject.Parse(data);
        }

        /// <summary>
        /// Loads data from database.
        /// </summary>
        /// <typeparam name="T">class where data is going to be serialized to</typeparam>
        /// <param name="Id">id field in elastic</param>
        /// <returns>Object T</returns>
        public T GetData<T>(string Id) where T : class
        {
            GetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
            var res = client.Get<T>(req);
            if (res.Found)
                return res.Source;
            else
                return (T)null;
        }

        public IEnumerable<dynamic> GetAllDataForQuery(string queryString, string scrollTimeout = "2m", int scrollSize = 1000)
        {
            var fixedQuery = Repositories.Searching.Tools.FixInvalidQuery(queryString,
                DatasetRepo.Searching.QueryShorcuts,
                DatasetRepo.Searching.QueryOperators);

            QueryContainer qc = DatasetRepo.Searching.GetSimpleQuery(this, fixedQuery);

            ISearchResponse<dynamic> initialResponse = client.Search<dynamic>
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
                ISearchResponse<dynamic> loopingResponse = client.Scroll<dynamic>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            client.ClearScroll(new ClearScrollRequest(scrollid));

            var expConverter = new ExpandoObjectConverter();

            var data = results
                .Select(m => JsonConvert.SerializeObject(m))
                .Select(s => (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(s, expConverter));

            return data;

            //return results;
        }

        public IEnumerable<T> GetAllData<T>(string scrollTimeout = "2m", int scrollSize = 1000) where T : class
        {
            ISearchResponse<dynamic> initialResponse = client.Search<dynamic>
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
                ISearchResponse<dynamic> loopingResponse = client.Scroll<dynamic>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            client.ClearScroll(new ClearScrollRequest(scrollid));

            var data = results
                .Select(m => JsonConvert.SerializeObject(m))
                .Select(j => JsonConvert.DeserializeObject<T>(j))
                .Cast<T>();
            ;

            return data;

            //return results;
        }

        public string GetData(string Id)
        {
            GetRequest req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
            var res = client.Get<object>(req);
            if (res.Found)
                return JsonConvert.SerializeObject(res.Source);
            else
            {
                req = new GetRequest(client.ConnectionSettings.DefaultIndex, Id);
                res = client.Get<object>(req);
                if (res.Found)
                    return JsonConvert.SerializeObject(res.Source);
                else
                    return (string)null;
            }
        }

        public bool DeleteData(string Id)
        {
            //DeleteRequest req = new DeleteRequest(client.ConnectionSettings.DefaultIndex, "data", Id);


            var res = client.LowLevel.Delete<StringResponse>(client.ConnectionSettings.DefaultIndex, Id);
            return res.Success;
        }

        public static bool ExistsDataset(string datasetId)
        {
            DataSet ds = new DataSet(datasetId, false);
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
            return Registration().name;
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

        public string SocialInfoTitle()
        {
            return Devmasters.TextUtil.ShortenText(Registration().name, 70);
        }

        public string SocialInfoSubTitle()
        {
            return "";
        }

        public string SocialInfoBody()
        {
            return InfoFact.RenderInfoFacts(InfoFacts(), 2, true, html: true);
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

        public long NumberOfRecords()
        {
            if (_numberOfRecords == -1)
            {
                var last = SearchData("*", 1, 1, "DbCreated desc", exactNumOfResults: true);
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

        DateTime _lastRecordUpdated = DateTime.MinValue;

        public DateTime LastRecordUpdated()
        {
            if (_lastRecordUpdated == DateTime.MinValue)
            {
                var last = SearchData("*", 1, 1, "DbCreated desc", exactNumOfResults: true);
                _numberOfRecords = last.Total;
                _lastRecordUpdated = last.Result.FirstOrDefault()?.DbCreated ?? DateTime.Now.AddYears(-10);
            }

            return _lastRecordUpdated;
        }

        InfoFact[] _infofacts = null;
        object lockInfoObj = new object();

        public InfoFact[] InfoFacts()
        {
            lock (lockInfoObj)
            {
                if (_infofacts == null)
                {
                    List<InfoFact> f = new List<InfoFact>();

                    DateTime dbCreated = Registration().created;
                    var first = SearchData("*", 1, 1, "DbCreated", exactNumOfResults: true);
                    var total = (int)first.Total;
                    var last = SearchData("*", 1, 1, "DbCreated desc");
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

            return _infofacts;
        }
    }
}