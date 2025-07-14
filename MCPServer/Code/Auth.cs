using HlidacStatu.LibCore.Extensions;
using ModelContextProtocol.Server;

namespace HlidacStatu.MCPServer.Code
{
    public static class Auth
    {
        private const string UnAuthorizedInstruction = "You need to provide Authorization header with token to use this MCP server. See https://api.hlidacstatu.cz/mcp";

        public static async Task ConfigureSessionCheckCookieAsync(HttpContext ctx, McpServerOptions mcpsOpt, CancellationToken cancellationToken)
        {
            var authUser = ctx.GetAuthPrincipal();
            if (authUser?.IsInRole("BetaTester") == true)
            //if (ctx.Request.Headers.TryGetValue("Authorization", out var token))
            { }
            else //no valid user
            {
                mcpsOpt.Capabilities = new ModelContextProtocol.Protocol.ServerCapabilities
                {
                    Tools = new ModelContextProtocol.Protocol.ToolsCapability() { ToolCollection = null },
                    Resources = new ModelContextProtocol.Protocol.ResourcesCapability() { ResourceCollection = null },                     
                };
                mcpsOpt.ServerInstructions = UnAuthorizedInstruction;
            }
        }

        public static async Task RunSessionCheckCookieAsync(HttpContext ctx, IMcpServer mcps, CancellationToken cancellationToken)  
        {
            // this is needed to set the user identity for the session
            if (ctx.Request.Headers.TryGetValue("Authorization", out var token))
            {
            }
            else
            {
                //mcps.ServerOptions.ServerInstructions = UnAuthorizedInstruction;
            }
            await mcps.RunAsync(cancellationToken);
        }

    }
}
