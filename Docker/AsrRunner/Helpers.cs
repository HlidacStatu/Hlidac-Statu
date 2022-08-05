using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace AsrRunner;

public static class Helpers
{
    public static Task<int> Bash(this string cmd, ILogger logger)
    {
        logger.Debug("Running bash command [{command}].", cmd);
        var source = new TaskCompletionSource<int>();
        var escapedArgs = cmd.Replace("\"", "\\\"");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        
        process.Exited += (sender, args) =>
        {
            string standardError = process.StandardError.ReadToEnd(); 
            if (!string.IsNullOrWhiteSpace(standardError))
                logger.Warning(standardError);

            string standardOutput = process.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(standardOutput))
                logger.Information(standardOutput);
            
            if (process.ExitCode == 0)
            {
                source.SetResult(0);
            }
            else
            {
                source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
            }

            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            logger.Error(e, "Command {command} failed", cmd);
            source.SetException(e);
        }

        return source.Task;
    }
}