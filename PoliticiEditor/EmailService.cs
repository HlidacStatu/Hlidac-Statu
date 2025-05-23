using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Serilog;

namespace PoliticiEditor
{
    public interface IEmailService
    {
        Task SendEmailAsync(string[] recipients, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _emailSettings;

        public EmailService(
            IOptions<SmtpSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string[] recipients, string subject, string message)
        {
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("",_emailSettings.FromAddress));

                foreach (string recipient in recipients)
                {
                    mimeMessage.To.Add(new MailboxAddress("", recipient));
                }
                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart("html")
                {
                    Text = message,
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(_emailSettings.Server);
                    // Note: only needed if the SMTP server requires authentication
                    //await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);
                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (Exception ex)
            {
                Log.ForContext<EmailService>().Error(ex, "Sending email failed");
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
    
    public class SmtpSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string FromAddress { get; set; }
    }
}