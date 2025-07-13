using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HlidacStatu.DS.Api.Dotace.Detail;

namespace HlidacStatu.DS.Api.Dotace
{
    public class ListItem
    {
        public string Subsidy_Id { get; set; } = null;
        public string Subsidy_Category { get; set; } = null;
        public string Subsidy_Name { get; set; } = null;
        public string Program_Name { get; set; } = null;

        public int? Year { get; set; } = null;

        public Subject Subsidy_Provider { get; set; } = null;

        public decimal? Amount_Received { get; set; } = null;

        public Subject Subsidy_Recipient { get; set; } = null;

        public string Subsidy_Type { get; set; } = null;

        public string Subsidy_Detail_Url { get; set; } = null;
    }
}
