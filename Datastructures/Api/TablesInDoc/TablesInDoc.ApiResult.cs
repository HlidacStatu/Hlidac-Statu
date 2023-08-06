using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public partial class TablesInDoc
    {


        public class ApiResult2
        {

            public Task task { get; set; }

            public TablesInDoc.Result[] results { get; set; }

        }

    }
}