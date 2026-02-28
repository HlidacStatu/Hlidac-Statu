using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Serilog;
using OfficeOpenXml;

namespace PlatyUploader;

public static class ParsePlatyUrednikuFile
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ParsePlatyUrednikuFile));
    private const int minimalAvgPlat = 30_000;
    private const int maximalAvgPlat = 400_000;

    public static async Task ProcessFolderAsync(string folderPath, DateTime denOdeslaniDatovky)
    {
        var directories = Directory.EnumerateDirectories(folderPath);
        foreach (var directory in directories)
        {
            var dirName = Path.GetFileName(directory);
            if (!DateTime.TryParseExact(dirName, "d.M.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
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

                await HandleFileUploadAsync(file, denOdeslaniDatovky, denPrijetiOdpovedi);
            }
        }
    }

    public static async Task HandleFileUploadAsync(string filePath, DateTime denOdeslaniDatovky,
        DateTime denPrijetiOdpovedi)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Logger.Warning("{FilePath} => ERROR: FILE NOT FOUND", filePath);
                return;
            }

            await using var fileStream = File.OpenRead(filePath);

            var parser = new PlatyUrednikuParser();
            var result = parser.Parse(fileStream, denOdeslaniDatovky, denPrijetiOdpovedi);

            Validate(result);
            var isPlatValid = ValidateAvgPlat(result);
            if (!isPlatValid)
            {
                Console.WriteLine($"Průměrný plat pro {filePath} je mimo range <30 000 a 350 000>");
                Console.WriteLine("Chcete tato data uložit? stiskněte klávesu A-Ano (uloží záznam - data jsou v pořádku) / klávesu N-Ne (přeskočí záznam)");
                while (true)
                {
                    var keyPressed = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (keyPressed == "a")
                    {
                        break;
                    }

                    if (keyPressed == "n")
                    {
                        Logger.Information("{FilePath} => SKIPPED", filePath);
                        return;
                    }
                    Console.WriteLine("Neplatná volba. Zadejte prosím 'A' pro pokračovat nebo 'N' pro přeskočit.");
                }
            }

            EnsureUniquePositionNames(result.Platy);
            await SaveAsync(result);

            var warningsPart = result.Warnings.Count > 0
                ? " - " + string.Join("; ", result.Warnings)
                : "";
            Logger.Information("{FilePath}{Warnings} => DONE", filePath, warningsPart);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "{FilePath} => ERROR: {ErrorMessage}", filePath, e.Message);
        }
    }

    private static void Validate(ParseUredniciResult result)
    {
        if (result.Platy.Count == 0)
            throw new InvalidOperationException("Soubor neobsahuje žádné záznamy platů.");

        if (result.Platy.Any(p => p.Rok != result.Rok))
            throw new InvalidOperationException("Rok hlavičky se neshoduje s rokem platu.");

        if (result.Platy.Any(p => p.PocetMesicu < 0 || p.PocetMesicu > 12))
            throw new InvalidOperationException("Nesmyslný počet odpracovaných měsíců.");
    }

    private static bool ValidateAvgPlat(ParseUredniciResult result)
    {
        // don't care about 0 plat
        var platyToCheck = result.Platy
            .Where(p => p.Plat > 0)
            .OrderBy(p => p.HrubyMesicniPlatVcetneOdmen)
            .ToList();
        // to be sure we have to also skip extremes
        platyToCheck = platyToCheck.Skip(1).SkipLast(1).ToList();

        if (platyToCheck.Count > 0)
        {
            var avgPlat = platyToCheck.Average(p => p.HrubyMesicniPlatVcetneOdmen);
            if (avgPlat is <= minimalAvgPlat or >= maximalAvgPlat)
            {
                return false;
            }
        }
        return true;
    }

    private static void EnsureUniquePositionNames(List<PuPlat> platy)
    {
        var uniqueNames = new HashSet<string>();
        foreach (var plat in platy)
        {
            var counter = 1;
            var candidateName = plat.NazevPozice;

            while (uniqueNames.Contains(candidateName))
            {
                candidateName = $"{plat.NazevPozice} {counter++}";
            }

            uniqueNames.Add(candidateName);
            plat.NazevPozice = candidateName;
        }
    }

    private static async Task SaveAsync(ParseUredniciResult result)
    {
        var idOrganizace = await LoadOrCreateOrganizaceAsync(result.Ds);

        var zduvodneniOdmen = false;

        foreach (var puPlat in result.Platy)
        {
            puPlat.IdOrganizace = idOrganizace;
            await PuRepo.UpsertPlatAsync(puPlat);
            if (!zduvodneniOdmen && puPlat.Odmeny > 0 && !string.IsNullOrWhiteSpace(puPlat.PoznamkaPlat))
            {
                zduvodneniOdmen = true;
            }
        }

        var metadata = new PuOrganizaceMetadata
        {
            IdOrganizace = idOrganizace,
            ZduvodneniMimoradnychOdmen = zduvodneniOdmen,
            DatumOdeslaniZadosti = result.DenOdeslaniDatovky,
            DatumPrijetiOdpovedi = result.DenPrijetiOdpovedi,
            Rok = result.Rok,
            Typ = PuOrganizaceMetadata.TypMetadat.PlatyUredniku,
        };

        await PuRepo.UpsertMetadataAsync(metadata);
    }

    private static async Task<int> LoadOrCreateOrganizaceAsync(string ds)
    {
        var origOrganizace = await PuRepo.GetFullDetailAsync(ds);
        var idOrganizace = origOrganizace?.Id ?? 0;

        if (idOrganizace == 0)
        {
            var newOrganizace = new PuOrganizace { DS = ds };
            await PuRepo.UpsertOrganizaceAsync(newOrganizace);
            idOrganizace = newOrganizace.Id;
        }

        return idOrganizace;
    }
}

public class ParseUredniciResult
{
    public required string? Instituce { get; init; }
    public required int Rok { get; init; }
    public required string? Ico { get; init; }
    public required string Ds { get; init; }
    public required DateTime DenOdeslaniDatovky { get; init; }
    public required DateTime DenPrijetiOdpovedi { get; init; }
    public required List<PuPlat> Platy { get; init; }
    public List<string> Warnings { get; } = new();
}

public class PlatyUrednikuParser : BasePlatyParser<PuPlat, PlatyUrednikuParser.ColumnName>
{
    public enum ColumnName
    {
        Pozice,
        Rok,
        OdpracovanychMesicu,
        Uvazek,
        Plat,
        Odmeny,
        NefinancniBonus,
        Poznamka
    }

    protected override (string Pattern, ColumnName ColumnKey)[] GetColumnPatterns() =>
    [
        ("Pozice", ColumnName.Pozice),
        ("Rok", ColumnName.Rok),
        ("Odpracováno", ColumnName.OdpracovanychMesicu),
        ("Výše úvazku", ColumnName.Uvazek),
        ("Plat", ColumnName.Plat),
        ("Odměny", ColumnName.Odmeny),
        ("Nefinanční", ColumnName.NefinancniBonus),
        ("Poznámka, např. zdůvodnění", ColumnName.Poznamka),
    ];

    public ParseUredniciResult Parse(Stream stream, DateTime denOdeslaniDatovky, DateTime denPrijetiOdpovedi)
    {
        ResetState();

        using var package = new ExcelPackage(stream);
        Sheet = package.Workbook.Worksheets[0];

        var (instituce, ico, ds, rok) = ParseHeader();
        DetectColumns();
        var platy = ParseAllPlaty();

        var result = new ParseUredniciResult
        {
            Instituce = instituce,
            Ico = ico,
            Ds = ds,
            Rok = rok,
            DenOdeslaniDatovky = denOdeslaniDatovky,
            DenPrijetiOdpovedi = denPrijetiOdpovedi,
            Platy = platy,
        };
        result.Warnings.AddRange(Warnings);

        return result;
    }

    protected override PuPlat? ParsePlatOnCurrentRow()
    {
        try
        {
            var nazevPozice = GetCellText(ColumnName.Pozice);
            if (string.IsNullOrWhiteSpace(nazevPozice))
                return null;

            var rokString = Utils.TrimBadChars(GetCellText(ColumnName.Rok));
            if (!int.TryParse(rokString, out var platRok))
            {
                Warnings.Add($"chybí rok pro řádek {CurrentRow}");
            }

            var odpracovanychMesicuString = Utils.TrimBadChars(GetCellText(ColumnName.OdpracovanychMesicu));
            if (!decimal.TryParse(odpracovanychMesicuString, out var odpracovanychMesicu))
            {
                Warnings.Add($"chybí odpracované měsíce pro řádek {CurrentRow}");
            }

            var uvazekString = Utils.TrimBadChars(GetCellText(ColumnName.Uvazek));
            if (!decimal.TryParse(uvazekString, out var uvazek))
            {
                Warnings.Add($"chybí úvazek pro řádek {CurrentRow}");
            }

            var hrubyPlatString = Utils.TrimBadChars(GetCellText(ColumnName.Plat));
            decimal? hrubyPlat = null;
            if (!string.IsNullOrWhiteSpace(hrubyPlatString))
            {
                hrubyPlat = HlidacStatu.Util.TextTools.GetDecimalFromText(hrubyPlatString);
            }

            if (hrubyPlat is null)
            {
                Warnings.Add($"neplatný hrubý plat pro řádek {CurrentRow}");
            }

            var odmenyString = Utils.TrimBadChars(GetCellText(ColumnName.Odmeny));
            decimal? odmeny = null;
            if (!string.IsNullOrWhiteSpace(odmenyString))
            {
                odmeny = HlidacStatu.Util.TextTools.GetDecimalFromText(odmenyString);
            }

            if (odmeny is null)
            {
                Warnings.Add($"neplatné odměny pro řádek {CurrentRow}");
            }

            var plat = new PuPlat
            {
                NazevPozice = nazevPozice,
                Rok = platRok,
                Uvazek = uvazek,
                PocetMesicu = odpracovanychMesicu,
                Plat = hrubyPlat,
                Odmeny = odmeny,
                NefinancniBonus = GetCellText(ColumnName.NefinancniBonus),
                PoznamkaPlat = GetCellText(ColumnName.Poznamka)
            };

            if (plat.Plat is null && plat.Odmeny is null)
            {
                Warnings.Add($"!NEVALIDNÍ PLAT PRO ŘÁDEK {CurrentRow}");
                return null;
            }

            return plat;
        }
        catch (Exception ex)
        {
            Warnings.Add($"neočekávaná chyba na řádku {CurrentRow}: {ex.Message}");
            return null;
        }
    }
}
