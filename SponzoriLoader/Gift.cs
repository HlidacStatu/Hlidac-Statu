﻿using HlidacStatu.Entities;

using System;

namespace SponzoriLoader
{
    public class Gift
    {
        public DateTime Date { get; set; }
        public string Party { get; set; }
        public string ICO { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Sponzoring.TypDaru GiftType { get; set; }
    }
}
