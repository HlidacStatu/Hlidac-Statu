using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Dotace
{
    public class Detail
    {
        public string Subsidy_Id { get; set; } = null;
        public string Subsidy_Category { get; set; } = null;
        public string Subsidy_Name { get; set; } = null;
        public string Subsidy_Code { get; set; } = null;
        public string Subsidy_Description { get; set; } = null;
        public string Program_Name { get; set; } = null;
        public string Program_Code { get; set; } = null;

        public int? Year { get; set; } = null;

        public Subject Subsidy_Provider { get; set; } = null;

        public decimal? Amount_Received { get; set; } = null;
        public decimal? Amount_Approved { get; set; } = null;
        public decimal? Amount_Refunded { get; set; } = null;

        public Subject Subsidy_Recipient { get; set; } = null;

        public string Subsidy_Type { get; set; } = null;
        public string Source_Url { get; set; } = "https://www.hlidacstatu.cz";
        public string Copyright { get; set; } = $"(c) {DateTime.Now.Year} Hlídač Státu z.ú.";

        public class Subject
        {
            public string Ico { get; set; } = null;
            public string Name { get; set; } = null;
        }

    }
}
