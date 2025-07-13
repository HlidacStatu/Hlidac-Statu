using HlidacStatu.DS.Api.MCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Osoba
{
    public class Detail : MCPBaseResponse
    {
        public string Person_Id { get; set; } = null;

        public string Name { get; set; } = null;
        public string Surname { get; set; } = null;
        public string Year_Of_Birth { get; set; } = null;

        public string Political_Involvement { get; set; } = "None";

        public string Photo_Url { get; set; } = null;

        public string Current_Political_Party { get; set; } = null;

        public Subject[] Involved_In_Companies { get; set; } = null;

        public Event[] Recent_Public_Activities{ get; set; } = null;
        public string Recent_Public_Activities_Description { get; set; } = null;

        public Stats[] Business_Contracts_With_Government { get; set; } = null;   

        public class Event
        {
            public string Period { get; set; } = null;
            public string Description { get; set; } = null;
            public string Type{ get; set; } = null;
        }
        public class Subject
        {
            public string Ico { get; set; } = null;
            public string Name { get; set; } = null;
        }

        public class Stats
        {
            public int Year { get; set; }
            public long Number_Of_Contracts { get; set; }
            public decimal Total_Contract_Value { get; set; }
        }
    }

}
