using System;
using System.Collections.Generic;
using System.Linq;

namespace WatchdogAnalytics.Models
{
    public class CompareDataModel
    {
        private bool _displayBaselineBoxPlot = true;
        private bool _displayBaselinePlotLines = true;

        public string Caption { get; set; } = "Přehled cen - souhrn";
        public string FirstColumnName { get; set; } = "Pozice";
        public string SubjectName { get; set; }
        public int? Height { get; set; }
        public List<JobStatistics> BasicData { get; set; }
        public List<JobStatistics> CompareWith { get; set; }

        public JobStatistics Baseline { get; set; }
        public bool ShowHelpDescription { get; set; } = true;

        public bool DisplayBaselineBoxPlot
        {
            get => _displayBaselineBoxPlot && Baseline != null;
            set => _displayBaselineBoxPlot = value;
        }

        public bool DisplayBaselinePlotLines
        {
            get => _displayBaselinePlotLines && Baseline != null;
            set => _displayBaselinePlotLines = value;
        }

        public bool ShowPocetSmluv { get; set; } = false;
        public bool ShowPocetCen { get; set; } = false;


        private string BaselineBoxplotData()
        {
            if (DisplayBaselineBoxPlot == false)
                return "";

            return $@"{{ low: {Baseline.LeftWhisk:F0}, q1: {Baseline.DolniKvartil:F0}, median: {Baseline.Median:F0},
                q3: {Baseline.HorniKvartil:F0}, high: {Baseline.RightWhisk:F0}, name: 'porovnani', fillColor: '#8FDB90',
                medianColor: '#00FF00', stemColor: '#00FF00',whiskerColor: '#00FF00'}},";
        }

        private string BasicBoxplotData()
        {
            return string.Join(",",
                BasicData.Select(j =>
                    $"[{j.LeftWhisk:F0},{j.DolniKvartil:F0},{j.Median:F0},{j.HorniKvartil:F0},{j.RightWhisk:F0}]"));
        }

        public string FillBoxplotData()
        {
            return BaselineBoxplotData() + BasicBoxplotData();
        }

        public string FillCategories()
        {
            string categories = "";
            if (DisplayBaselineBoxPlot)
                categories = $"'{Baseline.Name}',";
            
            return categories + string.Join(",", BasicData.Select(j => $"'{j.Name}'"));
        }

        public string FillCompareData()
        {
            
            if (CompareWith == null)
                return "";
            
            var pairedComparison = CompareWith
                .Where(x => BasicData.Any(y => y.Name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(x => x.Name).ToList();

            return string.Join(",",
                pairedComparison.Select(j => $"[{j.LeftWhisk:F0},{j.DolniKvartil:F0},{j.Median:F0},{j.HorniKvartil:F0},{j.RightWhisk:F0}]"));
            
        }
    }
}