﻿using Microsoft.AspNetCore.Html; // For extension methods.

using RazorEngine.Templating;

using System;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace HlidacStatu.XLib.Emails
{
    public class EmailMsg
    {
        private readonly ILogger _logger = Log.ForContext<EmailMsg>();

        public EmailMsg(string htmltemplate, string texttemplate)
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

        public string RenderView(string template)
        {
            if (string.IsNullOrEmpty(template))
                return "";
            var razorEngineConfig = new RazorEngine.Configuration.TemplateServiceConfiguration();
            razorEngineConfig.DisableTempFileLocking = true;
            razorEngineConfig.EncodedStringFactory = new RazorEngine.Text.RawStringFactory();
            var engineRazor = RazorEngineService.Create(razorEngineConfig); // new API

            //config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding.
            //config.EncodedStringFactory = new HtmlEncodedStringFactory(); // Html encoding.
            DynamicViewBag model = new DynamicViewBag(Model);

            try
            {
                var result = engineRazor.RunCompile(template, Devmasters.Crypto.Hash.ComputeHashToHex(template), null, model);

                return result;

            }
            catch (Exception e)
            {
                _logger.Error(e, "Razor template render");
                throw;
            }


        }

        public void SendMe()
        {
            try
            {
                using (MailMessage msg = new MailMessage(From, To))
                {
                    this.Model.EmailFooterText = this.EmailFooterText;
                    this.Model.EmailFooterHtml = this.EmailFooterHtml;
                    if (!string.IsNullOrEmpty(TextTemplate))
                    {
                        //msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(RenderView(this.TextTemplate), new System.Net.Mime.ContentType("text/plain")));
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
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
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

        public string RenderHtml()
        {
            try
            {
                return RenderView(HtmlTemplate);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Send email");
                throw;
            }
        }


        public IHtmlContent EmailFooterHtml = new HtmlString(@"<p style=""font-size:18px;""><b>Podpořte provoz Hlídače</b></p>
<p align=""left"">👉 <b>kontrolujeme politiky a úředníky</b>, zda s našimi penězi zacházejí správně.
<br>👉 <b>Stali jsme se důležitým zdrojem informací pro novináře</b>.
<br>👉 <b>Pomáháme státu zavádět moderní e-government</b>.
<br>👉 <b>Zvyšujeme transparentnost českého státu.</b>
</p>

<p><a href=""https://www.darujme.cz/projekt/1204683"">Podpořte nás i malým příspěvkem. Díky!</a></p>
<p>&nbsp;</p>
<p>&nbsp;</p>
<p><i>&#8608; Hlídáme je, protože si to zaslouží</i></p>");
        public string EmailFooterText = @"

PODPOŘTE PROVOZ HLÍDAČE

👉 kontrolujeme politiky a úředníky, zda s našimi penězi zacházejí správně.
👉 Stali jsme se důležitým zdrojem informací pro novináře.
👉 Pomáháme státu zavádět moderní e-government.
👉 Zvyšujeme transparentnost českého státu.


Podpořte nás i malým příspěvkem na https://www.darujme.cz/projekt/1204683. Děkujeme!


→ Hlídáme je, protože si to zaslouží
";






        public static EmailMsg CreateEmailMsgFromPostalTemplate(string templatename)
        {
            var email = new EmailMsg(GetEmailResourceString(templatename, "Html"), GetEmailResourceString(templatename, "Text"));

            email.Subject = GetValueFromPostalTemplate(GetEmailResourceString(templatename, null), "Subject");
            return email;
        }

        public static string GetValueFromPostalTemplate(string template, string key)
        {
            foreach (var line in (template ?? "").Split('\n', '\r'))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    string[] kv = line.Split(':');
                    if (kv.Length == 2)
                    {
                        if (kv[0].ToLower() == key.ToLower())
                            return kv[1].Trim();
                    }
                }
            }
            return string.Empty;
        }

        public static string GetEmailResourceString(string resourceKey, string format)
        {
            return GetEmailResourceString(typeof(EmailMsg).Assembly, @"HlidacStatu.XLib.Emails.Templates", resourceKey, format);
        }

        public static string GetEmailResourceString(Assembly sourceAssembly, string resourcePath, string resourceKey, string format)
        {
            var sFormat = format;
            if (sFormat != null)
                sFormat = "." + format;
            var name = string.Format("{0}.{1}{2}", resourcePath, resourceKey, sFormat);
            if (!name.EndsWith(".cshtml"))
                name = name + ".cshtml";

            if (sourceAssembly.GetManifestResourceNames().Contains(name))
            {
                using (var stream = sourceAssembly.GetManifestResourceStream(name))
                {
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(format))
                            return RemoveFirstLines(content, 1);
                        else
                            return content;
                    }
                }
            }
            else
                return null;
        }

        public static string RemoveFirstLines(string text, int linesCount)
        {
            var lines = Regex.Split(text, "\r\n|\r|\n").Skip(linesCount);
            return string.Join(Environment.NewLine, lines.ToArray());
        }
    }

}

