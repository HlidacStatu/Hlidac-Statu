using System;

namespace HlidacStatu.Lib.Data.External.Zabbix
{
    public class ZabHistoryItem
    {
        public string itemId { get; set; }
        public DateTime clock { get; set; }
        public decimal value { get; set; }
        public string svalue { get; set; }
    }
}