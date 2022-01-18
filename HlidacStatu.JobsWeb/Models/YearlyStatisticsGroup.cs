using System;
using System.Collections.Generic;

namespace HlidacStatu.JobsWeb.Models
{
    public class YearlyStatisticsGroup
    {
        public struct Key : IEquatable<Key>
        {
            public string Obor { get; set; }
            public int Rok { get; set; }

            public string UrlDecodedParams
            {
                get => $"rok={this.Rok}&obor={System.Net.WebUtility.UrlEncode(this.Obor)}";
            }
            public Services.JobService.SubjectYearDescription PerSubjectDesc
            {
                get => HlidacStatu.JobsWeb.Services.JobService.PerSubjectQuery(this.Obor,this.Rok);
            }

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