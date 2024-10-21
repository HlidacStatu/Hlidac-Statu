using HlidacStatu.LibCore.Extensions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace DotaceProcessor;

class Program
{
    //todo: subsidy entity přidat flat export
    
    static async Task Main(string[] args)
    {
        IConfiguration configuration = HlidacConfigExtensions.InitializeConsoleConfiguration(args);
        Devmasters.Config.Init(configuration);
        
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var fileProcessor = new FileProcessor();
        await fileProcessor.ProcessFilesAsync("Dotace");
    }
}