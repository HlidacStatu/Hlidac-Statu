using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HlidacStatu.DS.Api.Dotace.Detail;

namespace HlidacStatu.DS.Api.Osoba
{
    public class ListItem
    {

        public string Person_Id { get; set; } = null;

        public string Name { get; set; } = null;
        public string Surname { get; set; } = null;

        public string Year_Of_Birth { get; set; } = null;

        public string Political_Involvement { get; set; } = "None";

        public string Photo_Url { get; set; } = null;

        public string Current_Political_Party { get; set; } = null;

        public string Current_Political_Activity { get; set; } = null;
        public int? Involved_In_Companies_Count { get; set; } = null;

        public bool? Have_More_Details { get; set; } = null;
        
    }
}
