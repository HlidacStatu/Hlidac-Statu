using HlidacStatu.LibCore.Extensions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using PlatyUploader;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Usage: PlatyUploader <folderPath> <denOdeslaniDatovky>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  folderPath           Path to the root folder containing subdirectories with xlsx files");
    Console.WriteLine("  denOdeslaniDatovky   Date when the data message was sent (format: yyyy-MM-dd)");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  PlatyUploader \"C:\\Data\\Platy\" 2025-02-13");
    return 0;
}

if (args.Length < 2)
{
    Console.WriteLine("Error: Missing required arguments. Use --help for usage information.");
    return -1;
}

var folderPathArg = args[0];
var denOdeslaniDatovkyArg = args[1];


if (!Path.Exists(folderPathArg))
{
    Console.WriteLine($"Invalid folder path {folderPathArg}");
    return -1;
}

if (!DateTime.TryParse(denOdeslaniDatovkyArg, out DateTime denOdeslaniDatovky))
{
    Console.WriteLine($"Invalid date for denOdeslaniDatovky {denOdeslaniDatovkyArg}, use yyyy-MM-dd format");
    return -1;
}

ExcelPackage.License.SetNonCommercialOrganization("HlidacStatu");

IConfiguration configuration = HlidacConfigExtensions.InitializeConsoleConfiguration([]);
Devmasters.Config.Init(configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

Console.WriteLine("Running with params:");
Console.WriteLine($"  folderPath: {folderPathArg}");
Console.WriteLine($"  denOdeslaniDatovky: {denOdeslaniDatovky}");
Console.WriteLine("press enter to continue...");
Console.ReadLine();
Console.WriteLine("I ve got everything I need. Running app now.");

var directories = Directory.EnumerateDirectories(folderPathArg);
foreach (var directory in directories)
{
    var dirName = Path.GetFileName(directory);
    if (!DateTime.TryParseExact(dirName, "d.M.yyyy", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out DateTime denPrijetiOdpovedi))
    {
        Console.WriteLine($"{directory} - date cannot be parsed from directory name '{dirName}'");
        continue;
    }

    var subdirectories = Directory.EnumerateDirectories(directory);
    foreach (var subdirectory in subdirectories)
    {
        var files = Directory.EnumerateFiles(subdirectory, "*.xlsx");

        if (!files.Any())
        {
            Console.WriteLine($"{subdirectory} do not contain any xlsx files");
            continue;
        }

        var file = files
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .First();

        // if(file.Contains("Úřad pro zastupování státu ve věcech majetkových"))
            await ParsePlatyUrednikuFile.HandleFileUploadAsync(file, denOdeslaniDatovky, denPrijetiOdpovedi);
    }
}

Console.WriteLine("Everything done and ready.");

return 0;