using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    //hloupe reseni
    public class SingleConnection : IApiConnection
    {
        string url = null;
        private readonly string apiKey;

        public SingleConnection(string uri, string apiKey)
        {
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException("url");
            if (Uri.TryCreate(uri, UriKind.Absolute, out var xx) == false)
                throw new ArgumentException("invalid url");

            if (uri.EndsWith("/") || uri.EndsWith("\\"))
                uri = uri.Substring(0, uri.Length - 1);
            this.url = uri;
            this.apiKey = apiKey;
        }

        public void DeclareDeadEndpoint(string url)
        {
        }

        public string GetEndpointUrl()
        {
            return url;
        }

        public string GetApiKey() => this.apiKey;
    }
}
