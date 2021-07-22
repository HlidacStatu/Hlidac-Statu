using System.IO;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.Api
{
    public static class Helpers
    {
        public static async Task<string> ReadRequestBody(Stream req)
        {
            if (!req.CanRead)
                return "";
            
            string ret = "";
            if (req.CanSeek)
                req.Position = 0;

            using var stream = new StreamReader(req);
            ret = await stream.ReadToEndAsync();

            return ret;
        }


    }
}