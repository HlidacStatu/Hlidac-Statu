using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace HlidacStatu.JobsWeb.Models
{
    public class YearlyStatisticsGroup
    {
        public struct Key : IEquatable<Key>
        {
            public string Obor { get; set; }
            public int Rok { get; set; }


            public bool Equals(Key other)
            {
                return Obor == other.Obor && Rok == other.Rok;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Obor, Rok);
            }
        }

        public Key KeyInfo { get; set; }

        public List<JobStatistics> JobStatistics { get; set; } = new();
        public Dictionary<string, List<JobStatistics>> TagStatistics { get; set; } = new();
        public Dictionary<string, List<JobStatistics>> OdberateleStatistics { get; set; } = new();
        public Dictionary<string, List<JobStatistics>> DodavateleStatistics { get; set; } = new();
    }
}