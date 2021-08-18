using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public interface IApiConnection
    {
        string GetEndpointUrl();
        void DeclareDeadEndpoint(string url);
    }
}
