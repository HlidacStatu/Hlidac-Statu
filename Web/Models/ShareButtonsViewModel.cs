namespace HlidacStatu.Web.Models
{
    public class ShareButtonsViewModel
    {
        public string Url { get; set; }
        public string Text { get; set; }
        public string PrefixHtml { get; set; }
        public string BetweenButtonsHtml { get; set; }
        public string PostfixHtml { get; set; }
        
        public ShareButtonsViewModel(string url, string text, string prefixHtml = "", string betweenButtonsHtml = "", string postfixHtml = "")
        {
            Url = url;
            Text = text;
            PrefixHtml = prefixHtml;
            BetweenButtonsHtml = betweenButtonsHtml;
            PostfixHtml = postfixHtml;
        }

    }
}