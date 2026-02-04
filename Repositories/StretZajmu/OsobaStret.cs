using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.StretZajmu
{

    public class OsobaStret
    {
        public Osoba Osoba { get; set; }
        public Devmasters.DT.DateInterval Stret_za_obdobi { get; set; }
        public string ParagrafOdstavec { get; set; }

        public List<StretFirma> Strety { get; set; } = new();

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
    public class StretFirma
    {
        public Firma Firma { get; set; }
        public DS.Graphs.Graph.Edge Vazba { get; set; }

        public int VazbaDistance { get; set; }

        public Devmasters.DT.DateInterval Za_obdobi { get; set; }

        public StatisticsSubjectPerYear<Smlouva.Statistics.Data> SmlouvyStat { get; set; }
        public StatisticsSubjectPerYear<Firma.Statistics.Dotace> DotaceStat { get; set; }
        public StatisticsSubjectPerYear<Firma.Statistics.VZ> VZStat { get; set; }


    }
}
