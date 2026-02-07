using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HlidacStatu.Repositories.StretZajmu.Role;

namespace HlidacStatu.Repositories.StretZajmu
{

    public class OsobaStret
    {
        private string _id;
        public string Id { 
            get  { 
                if (_id == null 
                    && Osoba_with_Event?.Osoba != null 
                    && Stret_za_obdobi != null
                    && !string.IsNullOrEmpty(ParagrafOdstavec)
                    )
                {
                    _id = CalculateId();
                }
                return _id;
            }
            set => id = value; }
        string CalculateId()
        {
            var niceId = $"{Osoba_with_Event.Osoba.NameId}_{Stret_za_obdobi.From:yyyyMMdd}_{Stret_za_obdobi.To:yyyyMMdd}_{this.ParagrafOdstavec}";
            return Devmasters.Crypto.Hash.ComputeHashToBase64(niceId);
        }

        public required Osoba_With_Event Osoba_with_Event { get; init; }
        public required Devmasters.DT.DateInterval Stret_za_obdobi { get; init; }
        public required string ParagrafOdstavec { get; init; }

        public List<StretFirma> Strety { get; set; } = new();

        decimal _stat_smlouvySum = -1;
        public decimal Stat_SmlouvySum { 
            get {
                if (_stat_smlouvySum < 0)
                {
                    _stat_smlouvySum = Strety.Sum(s => s.SmlouvyStat.Summary().CelkovaHodnotaSmluv);
                }
                return _stat_smlouvySum;            
            } 
        }
        decimal _stat_smlouvyNum = -1;
        public decimal Stat_SmlouvyCount
        {
            get
            {
                if (_stat_smlouvyNum < 0)
                {
                    _stat_smlouvyNum = Strety.Sum(s => s.SmlouvyStat.Summary().PocetSmluv);
                }
                return _stat_smlouvyNum;
            }
        }
        decimal _stat_dotaceSum = -1;
        public decimal Stat_DotaceSum
        {
            get
            {
                if (_stat_dotaceSum < 0)
                {
                    _stat_dotaceSum = Strety.Sum(s => s.DotaceStat.Summary().CelkemPrideleno);
                }
                return _stat_dotaceSum;
            }
        }
        decimal _stat_dotaceNum = -1;
        private string id;

        public decimal Stat_DotaceCount
        {
            get
            {
                if (_stat_dotaceNum < 0)
                {
                    _stat_dotaceNum = Strety.Sum(s => s.DotaceStat.Summary().PocetDotaci);
                }
                return _stat_dotaceNum;
            }
        }


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
