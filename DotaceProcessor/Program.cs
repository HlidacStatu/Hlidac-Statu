using OfficeOpenXml;

namespace DotaceProcessor;

class Program
{
    //todo: Přidat zpracování excelu
    //todo: subsidy entity přidat flat export
    
    static async Task Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var fileProcessor = new FileProcessor();
        await fileProcessor.ProcessFilesAsync(@"path_to_files_directory");
    }
}