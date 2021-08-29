namespace HlidacStatu.XLib.Watchdogs
{
    public class Email
    {
        public static bool SendEmail(string toEmail, string subject, RenderedContent content, string fromEmail = "podpora@hlidacstatu.cz")
        {
            return Util.SMTPTools.SendEmail(subject, content.ContentHtml, content.ContentText, toEmail, fromEmail, false);
        }
    }
}
