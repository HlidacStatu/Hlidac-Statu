using System.Diagnostics;
using Serilog;

namespace AsrRunner;

public static class Helpers
{
    public static Task<int> BashAsync(this string cmd, ILogger logger)
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
        //process.OutputDataReceived += (sender, args) => { logger.Debug("bash: {msg}", args.Data); };
        //process.ErrorDataReceived+= (sender, args) => { logger.Warning("bash err: {msg}", args.Data); };

        process.Exited += (sender, args) =>
        {
            string standardError = process.StandardError.ReadToEnd(); 
            if (!string.IsNullOrWhiteSpace(standardError))
                logger.Warning("bash standartError: {standardError}", standardError);

            string standardOutput = process.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(standardOutput))
                logger.Information("bash standartOutput: {standardOutput}", standardOutput);
            
            if (process.ExitCode == 0)
            {
                logger.Warning("Command `{cmd}` succeded\nconsole:{standardOutput}\nerrors:{standardError}",
                    cmd, standardOutput, standardError);
                source.SetResult(0);
            }
            else
            {
                logger.Warning("Command `{cmd}` failed with exit code `{exitCode}`\nconsole:{standardOutput}\nerrors:{standardError}", 
                    cmd, process.ExitCode, standardOutput, standardError);
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
            logger.Error(e, "Command {command} failed with exception", cmd);
            source.SetException(e);
        }

        return source.Task;
    }
}