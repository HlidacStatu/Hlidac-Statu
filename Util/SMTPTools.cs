using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace HlidacStatu.Util
{
    public static class SMTPTools
    {

        public class EmbeddedImage
        {
            public string ContentId { get; set; } = Guid.NewGuid().ToString("N");
            public string ImageHtmlCode => @$"<img src='cid:{this.ContentId}'/>";
            public string FilePath { get; set; }
            public string ContentType { get; set; }
            public string ReplacementInMail { get; set; }

            LinkedResource _res = null;
            public LinkedResource GetEmbeddedImage()
            {
                if (_res == null)
                {
                    _res = new LinkedResource(this.FilePath, this.ContentType);
                    _res.ContentId = this.ContentId;
                }
                return _res;
            }
        }

        public static bool SendEmail(string subject,
            string htmlContent, string textContent,
            string toEmail,
            string fromEmail = "podpora@hlidacstatu.cz", bool bccToAdmin = false,
            IEnumerable<EmbeddedImage> images = null
            )
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
                        if (images?.Count() > 0)
                        {
                            foreach (var img in images)
                            {
                                htmlContent = htmlContent.Replace(img.ReplacementInMail, img.ImageHtmlCode);
                            }
                        }

                        var view = AlternateView.CreateAlternateViewFromString(htmlContent, new System.Net.Mime.ContentType("text/html"));
                        view.ContentType.CharSet = Encoding.UTF8.WebName;

                        if (images?.Count() > 0)
                        {
                            foreach (var img in images)
                            {
                                view.LinkedResources.Add(img.GetEmbeddedImage());
                            }
                        }


                        msg.AlternateViews.Add(view);
                    }


                    msg.Subject = subject;
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.SubjectEncoding = Encoding.UTF8;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        Consts.Logger.Info("Util.SMTPTools.SendEmail sent email via {smtpserver} to {recipient}", smtp.Host, msg.To);
                        if (bccToAdmin)
                            msg.Bcc.Add("michal@michalblaha.cz");
                        smtp.Send(msg);
                        return true;
                    }

                }
            }
            catch (Exception e)
            {

                Consts.Logger.Error("Util.SMTPTools.SendEmail sent email Error via {smtpserver} to {recipient}", e, Devmasters.Config.GetWebConfigValue("SmtpHost"), toEmail);
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
