// For extension methods.

using System;
using System.Net.Mail;
using System.Text;
using Serilog;

namespace HlidacStatu.XLib.Emails
{
    public class EmailMessageFromTemplate
    {
        private readonly ILogger _logger = Log.ForContext<EmailMessageFromTemplate>();
        public EmailMessageFromTemplate(string htmltemplate, string texttemplate)
        {
            HtmlTemplate = htmltemplate;
            TextTemplate = texttemplate;
        }

        public string HtmlTemplate { get; set; }
        public string TextTemplate { get; set; }

        public string To { get; set; }
        public string From { get; set; } = "podpora@HlidacStatu.cz";

        public string Subject { get; set; }

        public dynamic Model { get; set; } = new System.Dynamic.ExpandoObject();


        private string RenderView(string template)
        {
            try
            {
                var t = new Render.ScribanT(template);
                return t.Render(Model);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Scriban template render");
                throw;
            }
        }

        public void SendEmail()
        {
            try
            {
                using (MailMessage msg = new MailMessage(From, To))
                {
                    if (!string.IsNullOrEmpty(TextTemplate))
                    {
                        //msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(, new System.Net.Mime.ContentType("text/plain")));
                        var view = AlternateView.CreateAlternateViewFromString(RenderView(TextTemplate), new System.Net.Mime.ContentType("text/plain"));
                        view.ContentType.CharSet = Encoding.UTF8.WebName;
                        msg.AlternateViews.Add(view);

                    }
                    if (!string.IsNullOrEmpty(HtmlTemplate))
                    {
                        //msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(RenderView(this.HtmlTemplate), new System.Net.Mime.ContentType("text/html")));
                        var view = AlternateView.CreateAlternateViewFromString(RenderView(HtmlTemplate), new System.Net.Mime.ContentType("text/html"));
                        view.ContentType.CharSet = Encoding.UTF8.WebName;
                        msg.AlternateViews.Add(view);
                    }
                    msg.Subject = Subject;
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.SubjectEncoding = Encoding.UTF8;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        _logger.Information("Sending email to " + msg.To);
                        //msg.Bcc.Add("michal@michalblaha.cz");
                        smtp.Send(msg);
                    }
                }

            }
            catch (Exception e)
            {
                _logger.Error(e, "Send email");
#if DEBUG
                throw;
#endif
            }
        }
        public string RenderText()
        {
            try
            {
                return RenderView(TextTemplate);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Render text version");
                throw;
            }
        }

        public string RenderHtml()
        {
            try
            {
                return RenderView(HtmlTemplate);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Render html version");
                throw;
            }
        }


        public static string DefaultEmailFooterHtml = @"<p style=""font-size:18px;""><b>Podpořte provoz Hlídače</b></p>
<p align=""left"">👉 <b>kontrolujeme politiky a úředníky</b>, zda s našimi penězi zacházejí správně.
<br>👉 <b>Stali jsme se důležitým zdrojem informací pro novináře</b>.
<br>👉 <b>Pomáháme státu zavádět moderní e-government</b>.
<br>👉 <b>Zvyšujeme transparentnost českého státu.</b>
</p>

<p><a href=""https://www.darujme.cz/projekt/1204683"">Podpořte nás i malým příspěvkem. Díky!</a></p>
<p>&nbsp;</p>
<p>&nbsp;</p>
<p><i>&#8608; Hlídáme je, protože si to zaslouží</i></p>";
        public static string DefaultEmailFooterText = @"

PODPOŘTE PROVOZ HLÍDAČE

👉 kontrolujeme politiky a úředníky, zda s našimi penězi zacházejí správně.
👉 Stali jsme se důležitým zdrojem informací pro novináře.
👉 Pomáháme státu zavádět moderní e-government.
👉 Zvyšujeme transparentnost českého státu.


Podpořte nás i malým příspěvkem na https://www.darujme.cz/projekt/1204683. Děkujeme!


→ Hlídáme je, protože si to zaslouží
";






    }

}

