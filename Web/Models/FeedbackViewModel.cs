namespace HlidacStatu.Web.Models
{
    public class FeedbackViewModel
    {
        public string ButtonText { get; set; }
        public string? SelectOption { get; set; }
        public string? Style { get; set; }
        public string? IdPrefix { get; set; }
        public string[]? Options { get; set; }
        public bool MustAuth { get; set; }
        public string AddData { get; set; }

        public FeedbackViewModel(string buttonText,
            string? selectOption = null,
            string? style = null,
            string? idPrefix = null,
            string[]? options = null,
            bool mustAuth = false,
            string addData = "")
        {
            ButtonText = buttonText;
            SelectOption = selectOption;
            Style = style;
            IdPrefix = idPrefix;
            Options = options;
            MustAuth = mustAuth;
            AddData = addData;
        }
    }
}