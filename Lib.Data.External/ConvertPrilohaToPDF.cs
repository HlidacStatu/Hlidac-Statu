using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elastic.CommonSchema;

using HlidacStatu.Entities;

using Nest;

namespace HlidacStatu.Lib.Data.External
{
    public class ConvertPrilohaToPDF
    {
        public static byte[] PrilohaToPDF(string url)
        {
            using (Devmasters.Net.HttpClient.URLContent net = 
                new Devmasters.Net.HttpClient.URLContent(Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api")+$"/LibreOffice/ConvertFromUrl?targetFormat=pdf&url={System.Net.WebUtility.UrlEncode(url)}"))
            {
                net.RequestParams.Headers.Add("Authorization", Devmasters.Config.GetWebConfigValue("LibreOffice.Service.Api.Key"));

                net.Tries = 1;
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
