using Devmasters;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Serilog;
using OfficeOpenXml;

namespace PlatyUploader;

public static class ParsePlatyPolitikuFile
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ParsePlatyPolitikuFile));

    public static async Task ProcessFolderAsync(string folderPath, bool strictMode)
    {
        var directories = Directory.EnumerateDirectories(folderPath);
        foreach (var directory in directories)
        {
            var files = Directory.EnumerateFiles(directory, "*.xlsx");

            if (!files.Any())
            {
                Console.WriteLine($"{directory} does not contain any xlsx files");
                continue;
            }

            var file = files
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .First();

            await HandleFileUploadAsync(file, strictMode);
        }
    }

    public static async Task HandleFileUploadAsync(string filePath, bool strictMode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Logger.Warning("{FilePath} => ERROR: FILE NOT FOUND", filePath);
                return;
            }

            await using var fileStream = File.OpenRead(filePath);

            var parser = new PlatyPolitikuParser(strictMode);
            var result = parser.Parse(fileStream);

            Validate(result);

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

    private static void Validate(ParsePoliticiResult result)
    {
        if (result.Platy.Count == 0)
            throw new InvalidOperationException("Soubor neobsahuje žádné záznamy platů.");

        if (result.Platy.Any(p => p.Rok != result.Rok))
            throw new InvalidOperationException("Rok hlavičky se neshoduje s rokem platu.");

        if (result.Platy.Any(p => p.PocetMesicu < 0 || p.PocetMesicu > 12))
            throw new InvalidOperationException("Nesmyslný počet odpracovaných měsíců.");
    }

    private static bool IsMissingPlaty(ParsePoliticiResult result)
    {
        var platyToCheck = result.Platy.Sum(p => p.CelkoveRocniNakladyNaPolitika);

        return Math.Round(platyToCheck, 0) == 0;
    }

    private static async Task SaveAsync(ParsePoliticiResult result)
    {
        var idOrganizace = await LoadOrCreateOrganizaceAsync(result.Ds);

        foreach (var puPlat in result.Platy)
        {
            puPlat.IdOrganizace = idOrganizace;
            await PpRepo.UpsertPrijemPolitikaAsync(puPlat);
        }
    }

    private static async Task<int> LoadOrCreateOrganizaceAsync(string ds)
    {
        var origOrganizace = await PpRepo.GetOrganizaceOnlyAsync(ds);
        var idOrganizace = origOrganizace?.Id ?? 0;

        if (idOrganizace == 0)
        {
            var newOrganizace = new PuOrganizace { DS = ds };
            await PpRepo.UpsertOrganizaceAsync(newOrganizace);
            idOrganizace = newOrganizace.Id;
        }

        return idOrganizace;
    }
}

public class ParsePoliticiResult
{
    public required string? Instituce { get; init; }
    public required int Rok { get; init; }
    public required string? Ico { get; init; }
    public required string Ds { get; init; }
    public required List<PpPrijem> Platy { get; init; }
    public List<string> Warnings { get; } = new();
}

/// <summary>
/// Strict mode říká, jestli hodit error při chybějícím nameid, nebo považovat řádek za prázdný
/// </summary>
/// <param name="strictMode"></param>
public class PlatyPolitikuParser(bool strictMode) : BasePlatyParser<PpPrijem, PlatyPolitikuParser.ColumnName>
{
    public enum ColumnName
    {
        NameId,
        OdpracovanychMesicu,
        Plat,
        Odmeny,
        NefinancniBonusy,
        Poznamka,
        Funkce,
        Uvolneny,
        Prispevky,
        RepreNahrady,
        CestoNahrady,
        KanclNahrady,
        UbytovaciNahrady,
        AdministrativniNahrady,
        AsistentNahrady,
        TelefonNahrady,
    }

    private int _rok;

    protected override (string Pattern, ColumnName ColumnKey)[] GetColumnPatterns() =>
    [
        ("nameid", ColumnName.NameId),
        ("Odpracováno měsíců", ColumnName.OdpracovanychMesicu),
        ("Počet měsíců", ColumnName.OdpracovanychMesicu),
        ("Plat", ColumnName.Plat),
        ("Odměna/příjem", ColumnName.Plat),
        ("Odměny", ColumnName.Odmeny),
        ("Mimořádná odměna", ColumnName.Odmeny),
        ("Další příspěvky", ColumnName.Prispevky),
        ("Nefinanční bonus", ColumnName.NefinancniBonusy),
        ("Poznámka", ColumnName.Poznamka),
        ("Funkce", ColumnName.Funkce),
        ("Uvolněný", ColumnName.Uvolneny),
        ("Náhrady na reprezentaci", ColumnName.RepreNahrady),
        ("Náhrady cestovní", ColumnName.CestoNahrady),
        ("Náhrady na kancelář", ColumnName.KanclNahrady),
        ("Náhrady na ubytování", ColumnName.UbytovaciNahrady),
        ("Náhrady na administrativu", ColumnName.AdministrativniNahrady),
        ("Náhrady na asistenta", ColumnName.AsistentNahrady),
        ("Náhrady na telefon", ColumnName.TelefonNahrady),
    ];

    public ParsePoliticiResult Parse(Stream stream)
    {
        ResetState();

        using var package = new ExcelPackage(stream);
        Sheet = package.Workbook.Worksheets[0];

        var (instituce, ico, ds, rok) = ParseHeader();
        _rok = rok;
        DetectColumns();

        int enumCount = Enum.GetValues<ColumnName>().Length;
        if (ColumnMap.Count < enumCount)
        {
            throw new InvalidOperationException($"Nepodařilo se detekovat všechny sloupce.");
        }

        var platy = ParseAllPlaty();

        var result = new ParsePoliticiResult
        {
            Instituce = instituce,
            Ico = ico,
            Ds = ds,
            Rok = rok,
            Platy = platy,
        };
        result.Warnings.AddRange(Warnings);

        return result;
    }

    protected override PpPrijem? ParsePlatOnCurrentRow()
    {
        var nazevFunkce = GetCellText(ColumnName.Funkce);

        var nameId = GetCellText(ColumnName.NameId).ToLower().Trim();

        var platString = Utils.TrimBadChars(GetCellText(ColumnName.Plat));
        decimal? plat = null;
        if (!string.IsNullOrWhiteSpace(platString))
        {
            plat = HlidacStatu.Util.TextTools.GetDecimalFromText(platString);
        }

        // Prázndný řádek
        if (string.IsNullOrWhiteSpace(nazevFunkce) && string.IsNullOrWhiteSpace(nameId) &&
            string.IsNullOrWhiteSpace(platString))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(nameId))
        {
            if(strictMode)
                throw new InvalidOperationException($"Chybí {nameof(nameId)} na řádku {CurrentRow}.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(nazevFunkce))
        {
            throw new InvalidOperationException($"Chybí {nameof(nazevFunkce)} na řádku {CurrentRow}.");
        }

        var uvolnenyString = GetCellText(ColumnName.Uvolneny)?.Trim().RemoveDiacritics().ToLower();
        int? uvolneny = uvolnenyString switch
        {
            var s when string.IsNullOrWhiteSpace(s) => null,
            var s when s.StartsWith("uv", StringComparison.OrdinalIgnoreCase) => 1,
            var s when s.StartsWith("ne", StringComparison.OrdinalIgnoreCase) => 0,
            _ => null
        };

        var odmenyString = Utils.TrimBadChars(GetCellText(ColumnName.Odmeny));
        decimal? odmeny = null;
        if (!string.IsNullOrWhiteSpace(odmenyString))
        {
            odmeny = HlidacStatu.Util.TextTools.GetDecimalFromText(odmenyString);
        }

        var prispevkyString = Utils.TrimBadChars(GetCellText(ColumnName.Prispevky));
        decimal? prispevky = HlidacStatu.Util.TextTools.GetDecimalFromText(prispevkyString);

        var repreNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.RepreNahrady));
        decimal? repreNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(repreNahradyString);

        var cestoNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.CestoNahrady));
        decimal? cestoNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(cestoNahradyString);

        var kanclNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.KanclNahrady));
        decimal? kanclNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(kanclNahradyString);

        var ubytovaciNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.UbytovaciNahrady));
        decimal? ubytovaciNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(ubytovaciNahradyString);

        var administrativniNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.AdministrativniNahrady));
        decimal? administrativniNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(administrativniNahradyString);

        var asistentNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.AsistentNahrady));
        decimal? asistentNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(asistentNahradyString);

        var telefonNahradyString = Utils.TrimBadChars(GetCellText(ColumnName.TelefonNahrady));
        decimal? telefonNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(telefonNahradyString);
        
        var odpracovanychMesicuString = Utils.TrimBadChars(GetCellText(ColumnName.OdpracovanychMesicu));
        if (!decimal.TryParse(odpracovanychMesicuString, out var odpracovanychMesicu))
        {
            odpracovanychMesicu = 12;
        }
        
        PpPrijem politikPrijem = new()
        {
            NazevFunkce = nazevFunkce,
            Nameid = nameId.ToLower(),
            Rok = _rok,
            Uvolneny = uvolneny,
            Plat = plat,
            Odmeny = odmeny,
            Prispevky = prispevky,
            PocetMesicu = odpracovanychMesicu,
            NahradaAdministrativa = administrativniNahrady,
            NahradaAsistent = asistentNahrady,
            NahradaCestovni = cestoNahrady,
            NahradaKancelar = kanclNahrady,
            NahradaReprezentace = repreNahrady,
            NahradaTelefon = telefonNahrady,
            NahradaUbytovani = ubytovaciNahrady,
            NefinancniBonus = GetCellText(ColumnName.NefinancniBonusy),
            PoznamkaPlat = GetCellText(ColumnName.Poznamka),
            Status = PpPrijem.StatusPlatu.PotvrzenyPlat_od_organizace
        };

        return politikPrijem;
    }
}