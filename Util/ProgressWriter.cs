using System;
using Devmasters.Batch;
using Serilog;

namespace HlidacStatu.Util;

public static class ProgressWriter
{
    public static void Write(ActionProgressData data, ILogger logger)
    {
        long num = data.ProcessedItems;
        string str1 = num.ToString();
        num = data.TotalItems;
        string str2 = num.ToString();
        string str3 = str1 + "/" + str2;
        num = data.TotalItems;
        int totalWidth = num.ToString().Length * 2 + 5;
        var p3 = data.EstimatedFinish == DateTime.MinValue ? "" : data.EstimatedFinish.ToString("dd.MM HH:mm:ss.f");
        
        logger.Information($"{DateTime.Now.ToLongTimeString(),-12}: {str3.PadRight(totalWidth)} { (data.PercentDone / 100f),-9:P3}  End:{p3}");
    }
}