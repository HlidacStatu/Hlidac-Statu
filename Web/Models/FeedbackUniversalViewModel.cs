namespace HlidacStatu.Web.Models
{
    public class FeedbackUniversalViewModel
    {
        public string HeaderText { get; set; }

        public string SubText { get; set; }

        public string NoteLabel { get; set; }

        public string BtnText { get; set; }

        public string Url { get; set; }

        public bool MustAuth { get; set; }

        public string AddData { get; set; }

        public string Style { get; }
        public string IdPrefix { get; }

        public FeedbackUniversalViewModel(string headerText, string subText, string noteLabel,
            string btnText, string url, bool mustAuth, string addData)
        {
            Style = "btn btn-primary btn-sm";
            IdPrefix = $"m{System.Guid.NewGuid():N}";
            HeaderText = headerText;
            SubText = subText;
            NoteLabel = noteLabel;
            BtnText = btnText;
            Url = url;
            MustAuth = mustAuth;
            AddData = addData;
        }
    }
}