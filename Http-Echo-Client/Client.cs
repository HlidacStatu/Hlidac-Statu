using System;
using System.Threading.Tasks;

namespace Http_Echo
{
    public partial class Client
    {
        public static async Task<string> MyIPAddressAsync(Uri echoServerAddress, bool noException = true)
        {
            try
            {
                Response res= await Devmasters.Net.HttpClient.Simple.GetAsync<Response>(echoServerAddress.ToString());
                //  "ip": "::ffff:10.10.100.147",
                string ip = Devmasters.RegexUtil.GetRegexGroupValue(res.ip, @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", "ip");

                return ip;

            }
            catch (Exception e)
            {
                if (noException)
                    return "";
                throw;
            }
            
        }

    }
}
