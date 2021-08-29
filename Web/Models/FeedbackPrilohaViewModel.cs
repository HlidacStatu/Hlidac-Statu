namespace HlidacStatu.Web.Models
{
    public class FeedbackPrilohaViewModel
    {
        public string PrilohaId { get; set; }
        public string Style { get; set; }

        public FeedbackPrilohaViewModel(string prilohaId, string style = "btn btn-primary btn-sm")
        {
            PrilohaId = prilohaId;
            Style = style;
        }
    }
}