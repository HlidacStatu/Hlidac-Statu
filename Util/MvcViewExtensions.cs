using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HlidacStatu.Util
{
    public static class MvcViewExtensions
    {


        public static string _getCurrentUrl(this HttpRequest request, bool ignoreRequestQuerystring, params KeyValuePair<string,string>[] additionalParams)
        {
            if (request == null) return null;
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port ?? -1,
                Path = request.Path.ToString(),
                Query = request.QueryString.ToString()
            };

            System.Collections.Specialized.NameValueCollection query = ignoreRequestQuerystring ? new() : HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (var param in additionalParams)
            {
                query[param.Key] = param.Value;
            }

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="additionalParams">must be pairs of parameter name and parametervalue</param>
        /// <returns></returns>
        public static string GetCurrentUrl(this HttpRequest request, params string[] additionalParamPairs)
        {
            List<KeyValuePair<string, string>> addParams = null;
            if (additionalParamPairs?.Length > 0)
            {
                if (additionalParamPairs.Length % 2==1) 
                    throw new ArgumentOutOfRangeException(nameof(additionalParamPairs), "must be pairs of parameter name and parametervalue");
                addParams = new ();
                for(int i = 0; i < additionalParamPairs.Length; i=i+2)
                    addParams.Add(new KeyValuePair<string, string>(additionalParamPairs[i], additionalParamPairs[i+1]));
            }

            return GetCurrentUrl(request, addParams?.ToArray() );
        }
        public static string GetCurrentUrl(this HttpRequest request, params KeyValuePair<string, string>[] additionalParams)
        {
            var url = _getCurrentUrl(request, false , additionalParams);
            return url;
        }
        public static string GetCurrentUrlWithoutParam(this HttpRequest request, params KeyValuePair<string, string>[] additionalParams)
        {
            var url = _getCurrentUrl(request, true, additionalParams);
            return url;
        }
        public static string GetFirstQueryParameter(this IQueryCollection queryCollection, string parameterName)
        {
            if (queryCollection.TryGetValue(parameterName, out var par))
            {
                return par.FirstOrDefault();
            }
            return string.Empty;
        }
        public static string[] GetQueryParameters(this IQueryCollection queryCollection, string parameterName)
        {
            if (queryCollection.TryGetValue(parameterName, out var par))
            {
                return par.Select(m => m).ToArray();
            }
            return System.Array.Empty<string>();
        }
    }
}
