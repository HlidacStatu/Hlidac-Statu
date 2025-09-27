using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace HlidacStatu.Connectors
{
    public class DirectDB
    {
        private static readonly ILogger _logger = Log.ForContext<DirectDB>();
        
        public static string DefaultCnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");

        public static volatile Devmasters.DirectSql Instance = new Devmasters.DirectSql(DefaultCnnStr);


    }
}
