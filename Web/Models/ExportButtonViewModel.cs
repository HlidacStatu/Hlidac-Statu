namespace HlidacStatu.Web.Models
{
    public class ExportButtonViewModel
    {
        public string Typ {get; set;} 
        public string Query {get; set;}
        public string Order {get; set;} 
        public string Title {get; set;}
        public string Style {get; set;} 
        public string MoreParams {get; set;} 
        public string? GaPageEventId {get; set;}
            
        public ExportButtonViewModel(string typ, string query, 
            string order = "0", 
            string title = "Export výsledků hledání", 
            string style = "", 
            string moreParams = "", 
            string? gaPageEventId = null)
        {
            Typ = typ;
            Query = query;
            Order = order;
            Title = title;
            Style = style;
            MoreParams = moreParams;
            GaPageEventId = gaPageEventId;
        }

    }
}