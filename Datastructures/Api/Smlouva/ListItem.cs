using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HlidacStatu.DS.Api.Dotace.Detail;

namespace HlidacStatu.DS.Api.Smlouva
{
    public class ListItem
    {
        public string Contract_Id { get; set; } = null;
        public string Contract_Category { get; set; } = null;
        public string Contract_Name { get; set; } = null;
        
        public DateTime? Signed_On { get; set; } = null;

        public Subject Contract_Provider { get; set; } = null;

        public decimal? Amount_Received { get; set; } = null;

        public Subject[] Contract_Recipients { get; set; } = null;

        public string Legal_Risk_Level { get; set; }

        public string Subsidy_Type { get; set; } = null;

        public class Subject
        {
            public string Ico { get; set; } = null;
            public string Name { get; set; } = null;
        }

    }
}
