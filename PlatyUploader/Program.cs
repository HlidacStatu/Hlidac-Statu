using HlidacStatu.LibCore.Extensions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using PlatyUploader;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  PlatyUploader uredniku <folderPath> <denOdeslaniDatovky>");
    Console.WriteLine("  PlatyUploader politiku <folderPath>");
    Console.WriteLine();
    Console.WriteLine("Modes:");
    Console.WriteLine("  uredniku   Process government employee salary files");
    Console.WriteLine("  politiku   Process politician salary files");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  folderPath           Path to the root folder containing subdirectories with xlsx files");
    Console.WriteLine("  denOdeslaniDatovky   Date when the data request was sent (format: yyyy-MM-dd, uredniku only)");
    Console.WriteLine("  nameid=[strict|loose]   If strict, error is thrown when missing any nameid");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  PlatyUploader uredniku \"C:\\Data\\PlatyUredniku\" 2025-02-13");
    Console.WriteLine("  PlatyUploader politiku \"C:\\Data\\PlatyPolitiku\" nameid=strict");
    return 0;
}

var mode = args[0].ToLowerInvariant();

ExcelPackage.License.SetNonCommercialOrganization("HlidacStatu");

IConfiguration configuration = HlidacConfigExtensions.InitializeConsoleConfiguration([]);
Devmasters.Config.Init(configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

switch (mode)
{
    case "uredniku":
    {
        return 0;
        if (args.Length < 3)
        {
            Console.WriteLine("Error: uredniku mode requires <folderPath> <denOdeslaniDatovky>. Use --help for usage information.");
            return -1;
        }

        var folderPath = args[1];
        if (!Path.Exists(folderPath))
        {
            Console.WriteLine($"Invalid folder path {folderPath}");
            return -1;
        }

        if (!DateTime.TryParse(args[2], out DateTime denOdeslaniDatovky))
        {
            Console.WriteLine($"Invalid date for denOdeslaniDatovky {args[2]}, use yyyy-MM-dd format");
            return -1;
        }

        Console.WriteLine("Running with params:");
        Console.WriteLine($"  mode: uredniku");
        Console.WriteLine($"  folderPath: {folderPath}");
        Console.WriteLine($"  denOdeslaniDatovky: {denOdeslaniDatovky}");
        Console.WriteLine("press enter to continue...");
        Console.ReadLine();

        await ParsePlatyUrednikuFile.ProcessFolderAsync(folderPath, denOdeslaniDatovky);
        break;
    }
    case "politiku":
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: politiku mode requires <folderPath> and <nameid=[strict|loose]> params.");
            return -1;
        }

        var folderPath = args[1];
        if (!Path.Exists(folderPath))
        {
            Console.WriteLine($"Invalid folder path {folderPath}");
            return -1;
        }
        
        var nameIdArg = args[2].Split("=", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
        if (string.IsNullOrWhiteSpace(nameIdArg))
        {
            Console.WriteLine($"Invalid nameId {nameIdArg} arg");
            return -1;
        }
        bool strictMode = true;
        switch (nameIdArg)
        {
            case "strict":
                strictMode = true;
                break;
            case "loose":
                strictMode = false;
                break;
            default:
                Console.WriteLine($"Invalid nameId mode [{nameIdArg}]. use \"loose\" or \"strict\" arg value.");
                return -1;
        }

        Console.WriteLine("Running with params:");
        Console.WriteLine($"  mode: politiku");
        Console.WriteLine($"  folderPath: {folderPath}");
        Console.WriteLine("press enter to continue...");
        Console.ReadLine();

        await ParsePlatyPolitikuFile.ProcessFolderAsync(folderPath, strictMode);
        break;
    }
    default:
        Console.WriteLine($"Unknown mode '{mode}'. Use 'uredniku' or 'politiku'. Use --help for usage information.");
        return -1;
}

Console.WriteLine("Everything done and ready.");

return 0;
