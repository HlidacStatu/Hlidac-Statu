namespace HlidacStatu.Web.Models
{
    public class DynamicModalViewModel
    {
        public string DynamicContentRelUrl { get; set; }
        public string NazevOdkazu { get; set; }
        public string CssClass { get; set; }
        public string LoadingText { get; set; }
        public string Style { get; set; }

        public DynamicModalViewModel(string dynamicContentRelUrl, 
            string nazevOdkazu = "Opravit", 
            string cssClass = "btn btn-link", 
            string loadingText = "Nahrávám formulář", 
            string style = "")
        {
            DynamicContentRelUrl = dynamicContentRelUrl;
            NazevOdkazu = nazevOdkazu;
            CssClass = cssClass;
            LoadingText = loadingText;
            Style = style;
            
            if (!DynamicContentRelUrl.EndsWith("&"))
            {
                if (DynamicContentRelUrl.Contains("?"))
                {
                    DynamicContentRelUrl += "&";
                }
                else
                {
                    DynamicContentRelUrl += "?";
                }
            }
            
        }
    }
}