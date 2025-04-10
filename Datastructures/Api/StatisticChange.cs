using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public class StatisticChange
    {

        /// <summary>
        /// Sledovaný parametry, čistě informační
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// absolutní změna hodnoty parametru
        /// </summary>
        public decimal ValueChange { get; private set; } = 0;
        /// <summary>
        /// procentuální změna hodnoty parametru
        /// </summary>
        public decimal? PercentChange { get; private set; } = 0;

        /// <summary>
        /// Starší poměřovaný rok
        /// </summary>

        public int FirstYear { get; set; }

        /// <summary>
        /// novější poměřovaný rok
        /// </summary>

        public int LastYear { get; set; }

        /// <summary>
        /// Hodnota v prvním roce
        /// </summary>
        public decimal FirstValue { get; set; } = 0;

        /// <summary>
        /// Hodnota v posledním roce
        /// </summary>
        public decimal LastValue { get; set; } = 0;

        /// <summary>
        /// Textová reprezentace procentuální změny
        /// </summary>
        public string PercentChangeText { get; set; } = string.Empty;

        public StatisticChange() { }
        public StatisticChange(int firstYear, int lastYear, string parameter, decimal firstValue, decimal lastValue)
        {
            this.FirstYear = firstYear;
            this.LastYear = lastYear;
            this.ParameterName = parameter;
            this.FirstValue = firstValue;
            this.LastValue = lastValue;
            Init();
        }

        public void Init()
        {

            ValueChange = this.LastValue - this.FirstValue;

            if (FirstValue == 0)
            {
                PercentChange = null;
            }
            else
                PercentChange = ValueChange / FirstValue;


            PercentChangeText = RenderPercentChange();
        }

        public string RenderPercentChange()
        {
            if (PercentChange == null)
                return string.Empty;

            string baseText = $"o {PercentChange?.ToString("P2")} oproti roku {this.FirstYear}";
            string text;
            switch (PercentChange)
            {
                case decimal n when n > 0:
                    text = $"nárůst {baseText}";
                    break;
                case decimal n when n < 0:
                    text = $"pokles {baseText}";
                    break;
                default:
                    text = $"stejný objem jako v roce {this.FirstYear}";
                    break;
            }
            return text;
        }
    }

}
