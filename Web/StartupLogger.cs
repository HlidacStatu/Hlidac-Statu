using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HlidacStatu.Web;

public static class StartupLogger
{
    private const string WritePath = "C:/Data/Logs/HlidacStatu/Web/startup.log";
    private static readonly object _lock = new();
    
    public static void Write(string message)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        try
        {
            lock (_lock)
            {
                File.AppendAllText(WritePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] - {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // ignored
        }
    }
}