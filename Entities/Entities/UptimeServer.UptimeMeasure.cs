﻿using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        public class UptimeMeasure

        {
            public int itemId { get; set; }
            public DateTime clock { get; set; }
            public decimal value { get; set; }
            public string svalue { get; set; }
        }

    }
}
