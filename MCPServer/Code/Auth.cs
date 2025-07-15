using HlidacStatu.LibCore.Extensions;
using HlidacStatu.Repositories;
using IdentityModel.Client;
using ModelContextProtocol.Server;
using OpenTelemetry.Trace;
using System.ServiceModel;
using static Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster.ClusterCephStatus;
using static Corsinvest.ProxmoxVE.Api.Shared.Models.Vm.VmQemuAgentNetworkGetInterfaces;

namespace HlidacStatu.MCPServer.Code
{
    public static class Auth
    {
        private const string UnAuthorizedInstruction = "You need to provide Authorization header with token to use this MCP server. See https://api.hlidacstatu.cz/mcp";

        public static async Task ConfigureSessionCheckCookieAsync(HttpContext ctx, McpServerOptions mcpsOpt, CancellationToken cancellationToken)
        {
            var authUser = ctx.GetAuthPrincipal();
            if (authUser?.IsInRole("BetaTester") == true)
            {
                if (mcpsOpt.KnownClientInfo == null)
                    mcpsOpt.KnownClientInfo = new ModelContextProtocol.Protocol.Implementation()
                    {
                        Name = authUser.Identity.Name + "|"+ctx.GetRemoteIp(),
                        Version = ""
                    };
                else
                    mcpsOpt.KnownClientInfo.Name = authUser.Identity.Name;

                var audit = new Entities.Audit()
                {
                    machineName = Environment.MachineName,
                    applicationName = mcpsOpt.ServerInfo.Name + " " + mcpsOpt.ServerInfo.Version,
                    date = DateTime.Now,
                    operation = Entities.Audit.Operations.Call.ToString(),
                    userId = authUser.Identity.Name,
                    IP = ctx.GetRemoteIp(),
                    objectId = "starting MCP Session",
                    requestUrl = "https://mcp.api.hlidacstatu.cz",
                    statusCode = 200,
                };
                AuditRepo.Add(audit);
            }
            else //no valid user
            {
                mcpsOpt.Capabilities = new ModelContextProtocol.Protocol.ServerCapabilities
                {
                    Tools = new ModelContextProtocol.Protocol.ToolsCapability() { ToolCollection = null },
                    Resources = new ModelContextProtocol.Protocol.ResourcesCapability() { ResourceCollection = null },
                };
                mcpsOpt.ServerInstructions = UnAuthorizedInstruction;


                var audit = new Entities.Audit()
                {
                    machineName = Environment.MachineName,
                    applicationName = mcpsOpt.ServerInfo.Name + " " + mcpsOpt.ServerInfo.Version,
                    date = DateTime.Now,
                    operation = Entities.Audit.Operations.Call.ToString(),
                    IP = ctx.GetRemoteIp(),
                    method = "starting MCP Session",
                    exception = "Unauthorized user",
                    requestUrl = "https://mcp.api.hlidacstatu.cz",
                    statusCode = 401,
                };
                AuditRepo.Add(audit);

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
