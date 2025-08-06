using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Identity;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPOther
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPOther>();

        private readonly IHttpContextAccessor _httpCtx;

        public MCPOther(IHttpContextAccessor httpContextAccessor)
        {
            _httpCtx = httpContextAccessor;
        }

        [McpServerTool(//UseStructuredContent = false,
            Name = "ping",
            Title = "Simple Echo tool"),
        Description("Simple Echo tool.")]
        public string Ping(IMcpServer server, 
            [Description("text to send back")] string text)
        {
            _=AuditRepo.Add(Audit.Operations.Call, 
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(), 
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()),"",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), text),
                null);

            Console.WriteLine("HttpContext: " + _httpCtx?.HttpContext?.ToString());
            return "Pong: " + text;
        }


        [McpServerTool(
            Name = "send_feedback",
            Title = "Send feedback to Hlidac statu team"),
        Description("Send feedback to Hlídač státu team. Ask user for his email. Its mandatory parameter.")]
        public  string SendFeedback(IMcpServer server,
            [Description("Email address of user")]
            string user_email,
            [Description("Text of feedback message")]
            string text,
            [Description("Name of user, if available. If not, empty string is used.")]
            string from_name = "")
        {
            return AuditRepo.AddWithElapsedTimeMeasure(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), user_email, text, from_name),
                null, () =>
                {

                    string email = user_email.Trim();
                    if (Devmasters.TextUtil.IsValidEmail(user_email) == false)
                    {
                        email = "mcp@hlidacstatu.cz";
                    }
                    string to = "podpora@hlidacstatu.cz";
                    string subject = "Zprava z MCP API HlidacStatu.cz od AI";

                    string body = $@"
Zpráva z MCP API:

Od uzivatele:{user_email} 

text zpravy: {text}";
                    try
                    {
                        Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

                    }
                    catch (Exception ex)
                    {

                        _logger.Fatal(ex, "Cannot send email with {email} and {body}", email, body);
                        return "Feedback failed.";
                    }
                    return "Feedback sent. Thanks a lot";
                });
        }
    }
}
