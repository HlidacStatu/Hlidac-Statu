using Serilog.Events;

namespace AsrRunner;

public static class Global
{
    
    public const string LocalDirectoryPath = $"opt/app/files"; 
    
    public static string Hostname =>
        Environment.GetEnvironmentVariable(nameof(Hostname).ToUpper()) ?? 
        "-- missing hostname env variable --";
    
    public static string TaskQueueUrl =>
        Environment.GetEnvironmentVariable(nameof(TaskQueueUrl)) ??
        "https://www.hlidacstatu.cz/api/v2/internalq/Voice2TextGetTask";
    public static string ReportSuccessUrl =>
        Environment.GetEnvironmentVariable(nameof(ReportSuccessUrl)) ??
        "https://www.hlidacstatu.cz/api/v2/internalq/Voice2TextDone";
    public static string ReportFailureUrl =>
        Environment.GetEnvironmentVariable(nameof(ReportFailureUrl)) ??
        "https://www.hlidacstatu.cz/api/v2/internalq/Voice2TextFailed/true";
    public static string ApiKey =>
        Environment.GetEnvironmentVariable(nameof(ApiKey)) ?? 
        throw new ArgumentNullException(nameof(ApiKey));
    
    
    public static string FtpAddress =>
        Environment.GetEnvironmentVariable(nameof(FtpAddress)) ??
        "dm3.devmasters.cz";
    public static string FtpUserName =>
        Environment.GetEnvironmentVariable(nameof(FtpUserName)) ??
        throw new ArgumentNullException(nameof(FtpUserName));
    public static string FtpPassword =>
        Environment.GetEnvironmentVariable(nameof(FtpPassword)) ??
        throw new ArgumentNullException(nameof(FtpPassword));
    public static int FtpPort =>
        int.TryParse(Environment.GetEnvironmentVariable(nameof(FtpPort)), out var val) ? val : 990;
    
    
    public static string OutputFileExtension =>
        Environment.GetEnvironmentVariable(nameof(OutputFileExtension)) ??
        "ctm";
    public static string InputFileExtension =>
        Environment.GetEnvironmentVariable(nameof(InputFileExtension)) ??
        "mp3";
    
    
    public static int HttpRetryCount =>
        int.TryParse(Environment.GetEnvironmentVariable(nameof(HttpRetryCount)), out var val) ? val : 10;
    public static int HttpRetryDelayInMs =>
        int.TryParse(Environment.GetEnvironmentVariable(nameof(HttpRetryDelayInMs)), out var val) ? val : 150;


    public static LogEventLevel MinLogLevel => Environment.GetEnvironmentVariable(nameof(MinLogLevel)) switch
    {
        "verbose" => LogEventLevel.Verbose,
        "debug" => LogEventLevel.Debug,
        "information" => LogEventLevel.Information,
        "error" => LogEventLevel.Error,
        "fatal" => LogEventLevel.Fatal,
        _ => LogEventLevel.Warning
    };

    
}