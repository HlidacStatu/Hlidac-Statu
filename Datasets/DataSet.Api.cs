﻿using HlidacStatu.Entities;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json;

using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HlidacStatu.Datasets
{

    public partial class DataSet
    {
        public static class Api
        {
            public static async Task<ApiResponseStatus> UpdateAsync(Registration dataset, ApplicationUser user)
            {
                if (user == null) throw new ArgumentNullException(nameof(user));
                try
                {
                    string updatedBy = user.Email?.ToLower() ?? "";
                    string id = dataset.datasetId;
                    if (string.IsNullOrEmpty(id))
                        return ApiResponseStatus.DatasetNotFound;

                    var oldReg = await DataSetDB.Instance.GetRegistrationAsync(id);
                    if (oldReg == null)
                        return ApiResponseStatus.DatasetNotFound;

                    //var oldRegAuditable = oldReg.ToAuditJson();

                    if (string.IsNullOrEmpty(oldReg.createdBy))
                    {
                        oldReg.createdBy = updatedBy; //fix for old datasets without createdBy
                    }

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        var m = new MailMessage()
                        {
                            From = new MailAddress("info@hlidacstatu.cz"),
                            Subject = $"update DATASET registrace od " + updatedBy,
                            IsBodyHtml = false,
                            Body = Newtonsoft.Json.JsonConvert.SerializeObject(dataset)
                        };
                        m.BodyEncoding = System.Text.Encoding.UTF8;
                        m.SubjectEncoding = System.Text.Encoding.UTF8;
                        m.To.Add("michal@michalblaha.cz");
                        try
                        {
                            smtp.Send(m);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }


                    //use everything from newReg, instead of jsonSchema, datasetId
                    //update object
                    if (!string.IsNullOrWhiteSpace(dataset.jsonSchema)
                        && dataset.jsonSchema != oldReg.jsonSchema
                        && user.IsInRole("Admin")
                        )
                    {
                        //update jsonSchema
                    }
                    else
                        dataset.jsonSchema = oldReg.jsonSchema;


                    dataset.datasetId = oldReg.datasetId;
                    dataset.created = DateTime.Now;
                    bool skipallowWriteAccess = user.IsInRole("Admin");

                    //pokud se lisi autor (byl pri updatu zmodifikovan), muze to udelat pouze Admin
                    if (!skipallowWriteAccess && updatedBy != oldReg.createdBy)
                    {
                        return ApiResponseStatus.DatasetNoPermision;
                    }

                    if (updatedBy != oldReg?.createdBy?.ToLower())
                    {
                        if (!user.IsInRole("Admin"))
                            dataset.createdBy = updatedBy; //pokud updatovano nekym jinym nez autorem (a je mozne to modifikovat), pak createdBy se zmeni na autora. Pokud 
                    }
                    if (dataset.searchResultTemplate != null && !string.IsNullOrEmpty(dataset.searchResultTemplate?.body))
                    {
                        var errors = dataset.searchResultTemplate.GetTemplateErrors();
                        if (errors.Count > 0)
                        {
                            var err = ApiResponseStatus.DatasetSearchTemplateError;
                            err.error.errorDetail = string.Join("\n", errors);
                            throw new DataSetException(dataset.datasetId, err);
                        }
                    }

                    if (dataset.detailTemplate != null && !string.IsNullOrEmpty(dataset.detailTemplate?.body))
                    {
                        var errors = dataset.detailTemplate.GetTemplateErrors();
                        if (errors.Count > 0)
                        {
                            var err = ApiResponseStatus.DatasetDetailTemplateError;
                            err.error.errorDetail = string.Join("\n", errors);
                            throw new DataSetException(dataset.datasetId, err);
                        }
                    }

                    await DataSetDB.Instance.AddDataAsync(dataset, updatedBy, skipallowWriteAccess: skipallowWriteAccess);

                    return ApiResponseStatus.Valid(dataset);

                }
                catch (DataSetException dse)
                {
                    return dse.APIResponse;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Dataset API");
                    return ApiResponseStatus.GeneralExceptionError(ex);
                }
            }

            public static async Task<ApiResponseStatus> CreateAsync(Registration dataset, string updatedBy, string jsonSchemaInString = null)
            {
                try
                {
                    Registration reg = dataset;

                    if (reg.jsonSchema == null)
                    {
                        reg.jsonSchema = StringToJSchema(jsonSchemaInString).ToString();
                    }

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        var m = new MailMessage()
                        {
                            From = new MailAddress("info@hlidacstatu.cz"),
                            Subject = "Nova DATASET registrace od " + updatedBy,
                            IsBodyHtml = false,
                            Body = Newtonsoft.Json.JsonConvert.SerializeObject(reg)
                        };
                        m.BodyEncoding = System.Text.Encoding.UTF8;
                        m.SubjectEncoding = System.Text.Encoding.UTF8;

                        m.To.Add("michal@michalblaha.cz");
                        try
                        {
                            smtp.Send(m);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }

                    reg.created = DateTime.Now;
                    reg.createdBy = updatedBy;
                    reg.NormalizeShortName();

                    var newreg = await RegisterNewAsync(reg, updatedBy);

                    return new ApiResponseStatus() { valid = true, value = newreg };
                }
                catch (Newtonsoft.Json.JsonSerializationException jex)
                {
                    var status = ApiResponseStatus.DatasetItemInvalidFormat;
                    status.error.errorDetail = jex.Message;
                    return status;
                }
                catch (DataSetException dse)
                {
                    return dse.APIResponse;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Dataset API");
                    return ApiResponseStatus.GeneralExceptionError(ex);
                }
            }


            public static JSchema StringToJSchema(string data)
            {
                if (string.IsNullOrEmpty(data))
                    throw new DataSetException("", ApiResponseStatus.DatasetJsonSchemaMissing);

                try
                {
                    var jsch = JSchema.Parse(data);

                    jsch.AllowAdditionalProperties = false;
                    return jsch;
                }
                catch (Exception e)
                {
                    var apires = ApiResponseStatus.DatasetJsonSchemaError;
                    apires.error.errorDetail = e.ToString();
                    throw new DataSetException("", apires);
                }
            }
        }
    }
}
