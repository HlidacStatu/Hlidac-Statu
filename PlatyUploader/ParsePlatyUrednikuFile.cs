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

    private static void Validate(ParseResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Ds))
            throw new InvalidOperationException("V souboru není uvedená datová schránka.");

        if (result.Rok is null)
            throw new InvalidOperationException("Rok nenalezen v hlavičce.");

        if (result.Platy.Count == 0)
            throw new InvalidOperationException("Soubor neobsahuje žádné záznamy platů.");

        if (result.Platy.Any(p => p.Rok != result.Rok))
            throw new InvalidOperationException("Rok hlavičky se neshoduje s rokem platu.");

        if (result.Platy.Any(p => p.PocetMesicu < 0 || p.PocetMesicu > 12))
            throw new InvalidOperationException("Nesmyslný počet odpracovaných měsíců.");
    }
    
    private static bool ValidateAvgPlat(ParseResult result)
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

    private static async Task SaveAsync(ParseResult result)
    {
        var idOrganizace = await LoadOrCreateOrganizaceAsync(result.Ds!);

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
            Rok = result.Rok!.Value,
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

public class ParseResult
{
    public required string? Instituce { get; init; }
    public required int? Rok { get; init; }
    public required string? Ico { get; init; }
    public required string? Ds { get; init; }
    public required DateTime DenOdeslaniDatovky { get; init; }
    public required DateTime DenPrijetiOdpovedi { get; init; }
    public required List<PuPlat> Platy { get; init; }
    public List<string> Warnings { get; } = new();
}

public class PlatyUrednikuParser
{
    // Header search boundaries
    private const int HeaderSearchStartRow = 2;
    private const int HeaderSearchEndRow = 8;
    private const char HeaderSearchFirstColumn = 'A';
    private const char HeaderSearchLastColumn = 'F';

    // Column detection
    private const int MaxColumnDetectionAttempts = 20;
    private const char DataFirstColumn = 'A';
    private const char DataLastColumn = 'Z';

    // Row parsing
    private const int MaxConsecutiveEmptyRows = 10;

    private ExcelWorksheet _sheet = null!;
    private readonly Dictionary<ColumnName, char> _columnMap = new();
    private int _currentRow;
    private readonly List<string> _warnings = new();

    private enum ColumnName
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

    private static readonly (string Pattern, ColumnName Column)[] ColumnPatterns =
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

    public ParseResult Parse(Stream stream, DateTime denOdeslaniDatovky, DateTime denPrijetiOdpovedi)
    {
        _columnMap.Clear();
        _warnings.Clear();

        using var package = new ExcelPackage(stream);
        _sheet = package.Workbook.Worksheets[0];

        var (instituce, ico, ds, rok) = ParseHeader();
        DetectColumns();
        var platy = ParseAllPlaty();

        var result = new ParseResult
        {
            Instituce = instituce,
            Ico = ico,
            Ds = ds,
            Rok = rok,
            DenOdeslaniDatovky = denOdeslaniDatovky,
            DenPrijetiOdpovedi = denPrijetiOdpovedi,
            Platy = platy,
        };
        result.Warnings.AddRange(_warnings);

        return result;
    }

    private (string? instituce, string? ico, string? ds, int? rok) ParseHeader()
    {
        char? headerColumn = null;
        int? headerRow = null;

        for (var row = HeaderSearchStartRow; row <= HeaderSearchEndRow; row++)
        {
            for (var col = HeaderSearchFirstColumn; col <= HeaderSearchLastColumn; col++)
            {
                var text = _sheet.Cells[$"{col}{row}"].Text?.Trim();
                if (string.IsNullOrWhiteSpace(text) ||
                    text.Equals("Instituce", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                headerColumn = col;
                headerRow = row;
                break;
            }

            if (headerColumn is not null)
                break;
        }

        if (headerColumn is null || headerRow is null)
        {
            throw new InvalidOperationException("Hlavička nenalezena");
        }

        var instituce = _sheet.Cells[$"{headerColumn}{headerRow}"].Text?.Trim();
        var ico = _sheet.Cells[$"{headerColumn}{headerRow + 1}"].Text?.Trim();
        var ds = _sheet.Cells[$"{headerColumn}{headerRow + 2}"].Text?.Trim();
        var rokString = _sheet.Cells[$"{headerColumn}{headerRow + 3}"].Text;
        var rok = Utils.GetYearFromString(rokString);

        _currentRow = headerRow.Value + 3;

        return (instituce, ico, ds, rok);
    }

    private void DetectColumns()
    {
        for (var attempt = 0; attempt < MaxColumnDetectionAttempts; attempt++)
        {
            _currentRow++;

            for (var col = DataFirstColumn; col <= DataLastColumn; col++)
            {
                var cellText = _sheet.Cells[$"{col}{_currentRow}"].Text?.Trim();

                if (string.IsNullOrWhiteSpace(cellText))
                    continue;

                foreach (var (pattern, column) in ColumnPatterns)
                {
                    if (cellText.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _columnMap.TryAdd(column, col);
                        break;
                    }
                }
            }

            if (_columnMap.Count > 0)
                return;
        }

        throw new InvalidOperationException("Nenalezeny hlavičky sloupců s daty.");
    }

    private List<PuPlat> ParseAllPlaty()
    {
        var platy = new List<PuPlat>();
        var emptyRowStreak = 0;

        while (true)
        {
            _currentRow++;
            var plat = ParsePlatOnCurrentRow();

            if (plat is null)
            {
                emptyRowStreak++;
                if (emptyRowStreak >= MaxConsecutiveEmptyRows)
                    break;

                continue;
            }

            platy.Add(plat);
            emptyRowStreak = 0;
        }

        return platy;
    }

    private PuPlat? ParsePlatOnCurrentRow()
    {
        try
        {
            var nazevPozice = GetCellText(ColumnName.Pozice);
            if (string.IsNullOrWhiteSpace(nazevPozice))
                return null;

            var rokString = Utils.TrimBadChars(GetCellText(ColumnName.Rok));
            if (!int.TryParse(rokString, out var platRok))
            {
                _warnings.Add($"chybí rok pro řádek {_currentRow}");
            }

            var odpracovanychMesicuString = Utils.TrimBadChars(GetCellText(ColumnName.OdpracovanychMesicu));
            if (!decimal.TryParse(odpracovanychMesicuString, out var odpracovanychMesicu))
            {
                _warnings.Add($"chybí odpracované měsíce pro řádek {_currentRow}");
            }

            var uvazekString = Utils.TrimBadChars(GetCellText(ColumnName.Uvazek));
            if (!decimal.TryParse(uvazekString, out var uvazek))
            {
                _warnings.Add($"chybí úvazek pro řádek {_currentRow}");
            }

            var hrubyPlatString = Utils.TrimBadChars(GetCellText(ColumnName.Plat));
            decimal? hrubyPlat = null;
            if (!string.IsNullOrWhiteSpace(hrubyPlatString))
            {
                hrubyPlat = HlidacStatu.Util.TextTools.GetDecimalFromText(hrubyPlatString);
            }

            if (hrubyPlat is null)
            {
                _warnings.Add($"neplatný hrubý plat pro řádek {_currentRow}");
            }

            var odmenyString = Utils.TrimBadChars(GetCellText(ColumnName.Odmeny));
            decimal? odmeny = null;
            if (!string.IsNullOrWhiteSpace(odmenyString))
            {
                odmeny = HlidacStatu.Util.TextTools.GetDecimalFromText(odmenyString);
            }

            if (odmeny is null)
            {
                _warnings.Add($"neplatné odměny pro řádek {_currentRow}");
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
                _warnings.Add($"!NEVALIDNÍ PLAT PRO ŘÁDEK {_currentRow}");
                return null;
            }
            
            return plat;
        }
        catch (Exception ex)
        {
            _warnings.Add($"neočekávaná chyba na řádku {_currentRow}: {ex.Message}");
            return null;
        }
    }

    private string GetCellText(ColumnName columnName)
    {
        if (_columnMap.TryGetValue(columnName, out var column))
        {
            return _sheet.Cells[$"{column}{_currentRow}"].Text.Trim();
        }

        return string.Empty;
    }
}