using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    //hloupe reseni
    public class SingleConnection : IApiConnection
    {
        string url = null;
        public SingleConnection(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException("url");
            if (Uri.TryCreate(uri, UriKind.Absolute, out var xx) == false)
                throw new ArgumentException("invalid url");

            if (uri.EndsWith("/") || uri.EndsWith("\\"))
                uri = uri.Substring(0, uri.Length - 1);
            this.url = uri;
        }

        public void DeclareDeadEndpoint(string url)
        {
        }

        public string GetEndpointUrl()
        {
            return url;
        }
    }
}
