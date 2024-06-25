namespace HlidacStatu.Web.Models
{
    public class DynamicModalViewModel
    {
        public enum WidthSizeEnum
        {
            Small,
            Middle,
            Large,
            ExtraLarge

        }
        public string DynamicContentRelUrl { get; set; }
        public string NazevOdkazu { get; set; }
        public string CssClass { get; set; }
        public string LoadingText { get; set; }
        public string Style { get; set; }

        public WidthSizeEnum WidthSize { get; set; } = WidthSizeEnum.Middle; 

        public DynamicModalViewModel(string dynamicContentRelUrl,
            string nazevOdkazu = "Opravit",
            string cssClass = "btn btn-link",
            string loadingText = "Nahrávám formulář",
            string style = "", 
            WidthSizeEnum width = WidthSizeEnum.Middle)
        {
            DynamicContentRelUrl = dynamicContentRelUrl;
            NazevOdkazu = nazevOdkazu;
            CssClass = cssClass;
            LoadingText = loadingText;
            Style = style;
            WidthSize = width;

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