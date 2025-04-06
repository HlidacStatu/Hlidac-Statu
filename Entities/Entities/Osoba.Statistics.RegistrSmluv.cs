using HlidacStatu.DS.Graphs;
using HlidacStatu.Lib.Analytics;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class Osoba
    {
        public class Statistics
        {
            public class VerySimple
            {
                public string OsobaNameId { get; set; }
                public long PocetSmluv { get; set; } = 0;
                public decimal CelkovaHodnotaSmluv { get; set; } = 0;
                public int Year { get; set; } = 0;
            }

            public class RegistrSmluv
            {

                public string OsobaNameId { get; set; }
                public Relation.AktualnostType Aktualnost { get; set; }
                public Smlouva.SClassification.ClassificationsTypes? Obor { get; set; } = null;

                public Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> StatniFirmy { get; set; }
                public Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> SoukromeFirmy { get; set; }

                public Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> Neziskovky { get; set; }
                
                //migrace: prasárna
                public string[] _neziskovkyIcos { get; set; } = null;
                public StatisticsPerYear<Smlouva.Statistics.Data> _soukromeFirmySummary { get; set; } = null;
                public StatisticsPerYear<Smlouva.Statistics.Data> _statniFirmySummary { get; set; } = null;
                public StatisticsPerYear<Smlouva.Statistics.Data> _neziskovkySummary { get; set; } = null;


            }
            public class Dotace
            {

                public string OsobaNameId { get; set; }
                public Relation.AktualnostType Aktualnost { get; set; }

                public Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> StatniFirmy { get; set; } = new();
                public Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> SoukromeFirmy { get; set; } = new();
                public Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> Neziskovky { get; set; } = new();

                public StatisticsPerYear<Firma.Statistics.Dotace> AllDotaceSummary()
                {
                    var allDotace = StatniFirmy.Values                        
                        .Concat(SoukromeFirmy.Values)
                        .Concat(Neziskovky.Values);

                    StatisticsPerYear<Firma.Statistics.Dotace> summ = StatisticsSubjectPerYear<Firma.Statistics.Dotace>
                        .AggregateStats(allDotace);

                    return summ;
                }

            }


        }
    }
}
