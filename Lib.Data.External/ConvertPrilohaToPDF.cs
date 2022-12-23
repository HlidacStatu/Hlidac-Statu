using System;
using System.Collections.Generic;
using System.Net.Http;
using HlidacStatu.Entities;

namespace HlidacStatu.Lib.Data.External
{
    public class ConvertPrilohaToPDF
    {

        public static byte[] PrilohaToPDFfromFile(byte[] content)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(content), "file", "somefile");
            form.Add(new StringContent("anyfilename"), "filename");
            form.Add(new StringContent("pdf:writer_pdf_Export"), "targetformat");
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization",Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api.Key") }
            };
            var res = Devmasters.Net.HttpClient.Simple.PostAsync<ApiResult<byte[]>>(
                Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api") + "/LibreOffice/ConvertFromFile",
                form,
                headers: headers)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            if (res.Success)
                return res.Data;
            else
                throw new ApplicationException($"{res.ErrorCode} {res.ErrorDescription}");


        }

        public static byte[] PrilohaToPDFfromUrl(string url)
        {
            using (Devmasters.Net.HttpClient.URLContent net = 
                new Devmasters.Net.HttpClient.URLContent(Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api")+$"/LibreOffice/ConvertFromUrl?targetFormat=pdf&url={System.Net.WebUtility.UrlEncode(url)}"))
            {
                net.RequestParams.Headers.Add("Authorization", Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api.Key"));

                net.Tries = 20;
                net.TimeInMsBetweenTries = 1000 * 10;
                net.Timeout = 1000*120;
                var stat = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<byte[]>>(net.GetContent().Text);
                if (stat.Success)
                    return stat.Data;
                else
                    throw new ApplicationException($"{stat.ErrorCode} {stat.ErrorDescription}");
            }

        }
    }
}
