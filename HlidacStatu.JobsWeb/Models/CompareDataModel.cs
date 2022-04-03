using System.Collections.Generic;

namespace HlidacStatu.JobsWeb.Models
{
    public class CompareDataModel
    {
        public string Caption { get; set; } = "Přehled cen - souhrn";
        public string FirstColumnName { get; set; } = "Pozice";
        public string SubjectName { get; set; }
        public int? Height { get; set; }
        public List<JobStatistics> BasicData { get; set; }
        public List<JobStatistics> CompareWith { get; set; }

        public bool ShowPocetSmluv { get; set; } = false;
        public bool ShowPocetCen { get; set; } = false;

    }
}