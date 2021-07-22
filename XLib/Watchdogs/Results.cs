using System;
using System.Collections.Generic;

namespace HlidacStatu.XLib.Watchdogs
{
    public class Results
    {
        public string dataType = null;
        internal Results(IEnumerable<dynamic> results, long total, string searchUrl,
            DateTime? fromDate, DateTime? toDate, bool isValid, string datatype)
        {
            Items = results;
            SearchQuery = searchUrl;
            FromDate = fromDate;
            ToDate = toDate;
            Total = total;
            IsValid = isValid;
            dataType = datatype.ToLower();
        }
        public IEnumerable<dynamic> Items { get; private set; }
        public string SearchQuery { get; private set; }
        public long Total { get; set; }
        public bool IsValid { get; set; }
        public DateTime? FromDate { get; private set; }
        public DateTime? ToDate { get; private set; }


    }

}
