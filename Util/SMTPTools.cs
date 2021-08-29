using System;
using System.Net.Mail;
using System.Text;

namespace HlidacStatu.Util
{
    public static class SMTPTools
    {


        public static bool SendEmail(string subject,
            string htmlContent, string textContent,
            string toEmail,
            string fromEmail = "podpora@hlidacstatu.cz", bool bccToAdmin = false)
        {
            try
            {

                using (MailMessage msg = new MailMessage(fromEmail, toEmail))
                {
                    if (!string.IsNullOrEmpty(textContent))
                    {
                        var view = AlternateView.CreateAlternateViewFromString(textContent, new System.Net.Mime.ContentType("text/plain"));
                        view.ContentType.CharSet = Encoding.UTF8.WebName;
                        msg.AlternateViews.Add(view);
                    }
                    if (!string.IsNullOrEmpty(htmlContent))
                    {
                        var view = AlternateView.CreateAlternateViewFromString(htmlContent, new System.Net.Mime.ContentType("text/html"));
                        view.ContentType.CharSet = Encoding.UTF8.WebName;
                        msg.AlternateViews.Add(view);
                    }


                    msg.Subject = subject;
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.SubjectEncoding = Encoding.UTF8;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        Consts.Logger.Info("Sending email to " + msg.To);
                        if (bccToAdmin)
                            msg.Bcc.Add("michal@michalblaha.cz");
                        smtp.Send(msg);
                        return true;
                    }

                }
            }
            catch (Exception e)
            {

                Consts.Logger.Error("WatchDogs sending email error", e);
                return false;
            }

        }

        public static void SendSimpleMailToPodpora(string subject, string body, string replyTo)
        {
            string from = "podpora@hlidacstatu.cz";
            string to = "podpora@hlidacstatu.cz";

            using (var smtp = new SmtpClient())
            {
                smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                MailMessage msg = new MailMessage(from, to);
                msg.Bcc.Add("michal@michalblaha.cz");
                msg.Subject = subject;
                if (!string.IsNullOrEmpty(replyTo) && Devmasters.TextUtil.IsValidEmail(replyTo))
                    msg.ReplyToList.Add(new MailAddress(replyTo));
                msg.IsBodyHtml = false;
                msg.BodyEncoding = Encoding.UTF8;
                msg.SubjectEncoding = Encoding.UTF8;
                msg.Body = body;

                smtp.Send(msg);
            }
        }
    }
}
