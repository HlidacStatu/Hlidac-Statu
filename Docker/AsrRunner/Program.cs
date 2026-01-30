using AsrRunner;
using Devmasters.Log;
using FluentFTP;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(Global.MinLogLevel)
    .WriteTo.Console()
    .AddLogStash(new Uri(Global.LogStashUrl))
    .Enrich.WithProperty("hostname", Global.Hostname)
    .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString())
    .Enrich.WithProperty("application_name", "AsrRunner")
    .CreateLogger();

var logger = Log.ForContext<Program>();
var failSave = new FailSaveCounter(5);

// Graceful shutdown
logger.Information("Starting up...");
var applicationCts = new CancellationTokenSource();
Console.CancelKeyPress += (_, consoleCancelEventArgs) =>
{
    // Tell .NET to not terminate the process
    consoleCancelEventArgs.Cancel = true;

    logger.Information("Received SIGINT (Ctrl+C)");
    if (!applicationCts.IsCancellationRequested)
    {
        applicationCts.Cancel();
    }
};
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    logger.Information("Received SIGTERM");
    if (!applicationCts.IsCancellationRequested)
    {
        applicationCts.Cancel();
    }
};

// Necessary configuration
logger.Debug("Setting up services");
using var taskQueueService = new TaskQueueService(logger);

using var ftpClient = new FtpClient(Global.FtpAddress, Global.FtpUserName, Global.FtpPassword, Global.FtpPort);
ftpClient.Config.RetryAttempts = 5;
ftpClient.Config.NoopInterval = 30_000;
ftpClient.Config.EncryptionMode = FtpEncryptionMode.None;
//ftpClient.Config.ValidateAnyCertificate = true;
//ftpClient.ValidateCertificate += (control, e) => {
//    e.Accept = true;
//};

// Application loop
logger.Debug("Running main code");
while (!applicationCts.IsCancellationRequested)
{
    // create directory
    Directory.CreateDirectory(Global.LocalDirectoryPath);

    QueueItem? queueItem;

    try
    {
        if (System.Diagnostics.Debugger.IsAttached || Global.Debugme == "true")
            queueItem = new QueueItem() { ItemId = "3d687233627630e593bcdeb6ff5c2e3d", Dataset = "vyjadreni-politiku" };
        else
            queueItem = await taskQueueService.GetNewTaskAsync(applicationCts.Token);

        if (queueItem == null)
        {
            await Task.Delay(Global.DelayIfServerHasNoTaskInSec * 3000);
            continue;
        }
        if (string.IsNullOrEmpty(queueItem.Dataset) || string.IsNullOrEmpty(queueItem.ItemId))
        {
            await Task.Delay(Global.DelayIfServerHasNoTaskInSec * 1000);
            continue;
        }
    }
    catch (Exception e)
    {
        logger.Error(e, "Error occured in GetNewTaskAsync");
        await Task.Delay(Global.DelayIfServerHasNoTaskInSec * 1000);
        continue;
    }

    if (queueItem is null)
    {
        await Task.Delay(Global.DelayIfServerHasNoTaskInSec * 3000);
        continue;
    }

    logger.Information("New task with queue item {@queueItem} received.", queueItem);
    string inputFileLocal = $"{Global.LocalDirectoryPath}/{queueItem.ItemId}.{Global.InputFileExtension}";
    string inputFileFtp = $"/{queueItem.Dataset}/{queueItem.ItemId}.{Global.InputFileExtension}";
    string outputFileLocal = $"{Global.LocalDirectoryPath}/{queueItem.ItemId}.{Global.OutputFileExtension}";
    string outputFileFtp = $"/{queueItem.Dataset}/{queueItem.ItemId}.{Global.OutputFileExtension}";

    try
    {
        logger.Debug("Checking ftp if task [{fileName}] was completed before.", outputFileFtp);
        ftpClient.Connect();
        if (ftpClient.FileExists(outputFileFtp))
        {
            logger.Information("This task [{fileName}] was already completed before.", outputFileFtp);

            if (System.Diagnostics.Debugger.IsAttached || Global.Debugme == "true")
            { }
            else
                await taskQueueService.ReportSuccessAsync(applicationCts.Token);
            continue;
        }

        logger.Debug("Downloading [{fileName}] file from ftp", inputFileFtp);
        ftpClient.DownloadFile(inputFileLocal, inputFileFtp);
        ftpClient.Disconnect();
        logger.Debug("File downloaded.");

        // run ASR
        logger.Debug("Calling ASR script cd /opt/app/ && ./process.sh for [{filename}]", inputFileFtp);
        var processResult = await "cd /opt/app/ && ./process.sh".BashAsync(logger);
        logger.Debug("Called ASR script cd /opt/app/ && ./process.sh for [{filename}]", inputFileFtp);
        if (processResult != 0)
        {
            logger.Error("ASR resulted with error. Returned {processResult}.", processResult);
        }

        logger.Debug("Uploading [{fileName}] file to ftp", outputFileLocal);
        ftpClient.Connect();
        ftpClient.UploadFile(outputFileLocal, outputFileFtp);

        logger.Debug("File [{fileName}] uploaded to ftp", outputFileLocal);
        long fileSize = (new FileInfo(outputFileLocal)).Length;
        long uploadedFileSize = ftpClient.GetFileSize(outputFileFtp);
        ftpClient.Disconnect();

        if (fileSize == uploadedFileSize)
        {
            if (System.Diagnostics.Debugger.IsAttached || Global.Debugme == "true")
            { }
            else
                await taskQueueService.ReportSuccessAsync(applicationCts.Token);
            logger.Information("Task [{fileName}] successfully completed.", outputFileFtp);
            failSave.ResetCounter(); // everything went right
        }
        else
        {
            logger.Warning("Task [{fileName}] failed. FileSize {fileSize} != UploadedSize {uploadedFileSize}",
                outputFileFtp, fileSize, uploadedFileSize);
            throw new Exception("Uploaded size is not equal to the sent size.");
        }

    }
    catch (Exception exception)
    {
        logger.Warning(exception, "Task [{fileName}] failed.", outputFileFtp);

        if (System.Diagnostics.Debugger.IsAttached || Global.Debugme == "true")
        { }
        else
            await taskQueueService.ReportFailureAsync(applicationCts.Token);

        failSave.ReportFail(); //If too many fails occurs, it throws an exception
    }
    finally
    {
        logger.Debug("Cleaning local folder [{folder}]", Global.LocalDirectoryPath);
        Directory.Delete(Global.LocalDirectoryPath, true);

        Cleanup();
    }
}

logger.Information("Terminating application.");
await Log.CloseAndFlushAsync();

void Cleanup()
{
    logger.Debug("Running cleanup of ASR folders");
    // this needs to be cleaned up, because if problem occurs in process.sh (from Czech-ASR)
    // then these commands are not run and it goes to the fail spiral
    var model_dir = "/opt/app/exp/chain/cnn_tdnn";

    var dirsToClean = new List<string>()
    {
        "/opt/app/data/test",
        "/opt/app/tmp",
        "/opt/app/exp/nnet3/ivectors",
        $"{model_dir}/decode",
        $"{model_dir}/rescore"
    };

    foreach (var directory in dirsToClean)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
    logger.Debug("cleanup finished");
}
