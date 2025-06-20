namespace HlidacStatu.MCPServer.Tools;

using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed class ReverseEchoTool
{
    [McpServerTool(Name ="ReverseEcho"), Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message)
    {
        return "-- " + new string(message.Reverse().ToArray()) + " --";
    }
}