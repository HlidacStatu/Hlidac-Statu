using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;
using System;
using Microsoft.AspNetCore.Mvc;

namespace WatchdogAnalytics.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class KontaktModel : PageModel
    {
        public void OnGet()
        {
        }

        public void OnPost(string from, string text)
        {
            if (string.IsNullOrEmpty(from))
            {
                return;
            }
            using (SmtpClient smtp = new SmtpClient())
            {
                smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                var m = new MailMessage()
                {
                    From = new MailAddress(from),
                    Subject = "Nova zprava z WatchdogAnalytics.cz od " + from,
                    IsBodyHtml = false,
                    Body = text
                };
                m.BodyEncoding = System.Text.Encoding.UTF8;
                m.SubjectEncoding = System.Text.Encoding.UTF8;

                m.To.Add("podpora@hlidacstatu.cz");
                m.CC.Add("michal@michalblaha.cz");
                try
                {
                    smtp.Send(m);
                }
                catch (Exception)
                {
                    // ignored
                }

            }
        }
    }
}