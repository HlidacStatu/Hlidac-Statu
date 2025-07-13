using ModelContextProtocol.Server;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPOther
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPOther>();

        [McpServerTool(
            Name = "send_feedback",
            Title = "Send feedback to Hlidac statu team"),
        Description("Send feedback to Hlídač státu team. Ask user for his email. Its mandatory parameter.")]
        public static string SendFeedback(string from_email, string text, string from_name = "")
        {
            string email = from_email.Trim();
            if (Devmasters.TextUtil.IsValidEmail(from_email) == false)
            {
                email = "mcp@hlidacstatu.cz";
            }
            string to = "podpora@hlidacstatu.cz";
            string subject = "Zprava z MCP API HlidacStatu.cz od AI";

            string body = $@"
Zpráva z MCP API:

Od uzivatele:{from_email} 

text zpravy: {text}";
            try
            {
                Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

            }
            catch (Exception ex)
            {

                _logger.Fatal(ex,"Cannot send email with {email} and {body}",email, body);
                return "Feedback failed.";
            }
            return "Feedback sent. Thanks a lot";
        }
    }
}
