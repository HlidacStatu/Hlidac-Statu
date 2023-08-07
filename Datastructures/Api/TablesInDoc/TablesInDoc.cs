using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public partial class TablesInDoc
    {
        public static string TablesInDocProcessingQueueName = "tablesInDoc2Process";
        public enum Commands
        {
            stream,
            lattice
        }

        public enum Formats
        {
            CSV,
            JSON,
            HTML,
            EXCEL,
            SQLITE
        }

    }
}
