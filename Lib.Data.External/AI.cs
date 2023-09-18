using Codeproject.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Codeproject.AI.Client;

namespace HlidacStatu.Lib.Data.External
{
    public class AI
    {

        public enum RemoveBackgroundStyles
        {
            General,
            Person,
        }
        public static async Task<byte[]> RemoveBackgroundAsync(Uri apiEndPoint, byte[] bytesContent, RemoveBackgroundStyles style)
        {
            Dictionary<string,string> parameters = new Dictionary<string,string>();

            switch (style)
            {
                case RemoveBackgroundStyles.Person:
                    parameters.Add("model", "u2net_human_seg"); //use A pre-trained model for human segmentation
                    parameters.Add("a", "true"); //Enable Alpha Matting
                    break;
            case RemoveBackgroundStyles.General:
            default:
                    break;
            }

            try
            {

            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytesContent), "file", "anyfile.jpg");
            foreach (var kv in parameters)
                form.Add(new StringContent(kv.Value), kv.Key);

            byte[] res = await Devmasters.Net.HttpClient.Simple.PostRawBytesAsync(apiEndPoint.AbsoluteUri,form,timeout: TimeSpan.FromMinutes(5));

            return res;
            }
            catch (Exception e)
            {

                throw new ApiResponseException(e.ToString());
            }
        }
    }
}
