using Devmasters;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Serilog;
using OfficeOpenXml;

namespace PlatyUploader;

public static class ParsePlatyPolitikuFile
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ParsePlatyPolitikuFile));

    public static async Task ProcessFolderAsync(string folderPath)
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

            await HandleFileUploadAsync(file);
        }
    }

    public static async Task HandleFileUploadAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Logger.Warning("{FilePath} => ERROR: FILE NOT FOUND", filePath);
                return;
            }

            await using var fileStream = File.OpenRead(filePath);

            var parser = new PlatyPolitikuParser();
            var result = parser.Parse(fileStream);

            Validate(result);
            if (IsMissingPlaty(result))
            {
                Console.WriteLine($"V tabulce {filePath} jsou všechny platy nulové.");
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

        var zduvodneniOdmen = false;

        foreach (var puPlat in result.Platy)
        {
            puPlat.IdOrganizace = idOrganizace;
            await PpRepo.UpsertPrijemPolitikaAsync(puPlat);
            if (!zduvodneniOdmen && puPlat.Odmeny > 0 && !string.IsNullOrWhiteSpace(puPlat.PoznamkaPlat))
            {
                zduvodneniOdmen = true;
            }
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

public class PlatyPolitikuParser : BasePlatyParser<PpPrijem>
{
    private const string ColNameId = "NameId";
    private const string ColOdpracovanychMesicu = "OdpracovanychMesicu";
    private const string ColPlat = "Plat";
    private const string ColOdmeny = "Odmeny";
    private const string ColNefinancniBonusy = "NefinancniBonusy";
    private const string ColPoznamka = "Poznamka";
    private const string ColFunkce = "Funkce";
    private const string ColUvolneny = "Uvolneny";
    private const string ColPrispevky = "Prispevky";
    private const string ColRepreNahrady = "RepreNahrady";
    private const string ColCestoNahrady = "CestoNahrady";
    private const string ColKanclNahrady = "KanclNahrady";
    private const string ColUbytovaciNahrady = "UbytovaciNahrady";
    private const string ColAdministrativniNahrady = "AdministrativniNahrady";
    private const string ColAsistentNahrady = "AsistentNahrady";
    private const string ColTelefonNahrady = "TelefonNahrady";

    private int _rok;

    protected override (string Pattern, string ColumnKey)[] GetColumnPatterns() =>
    [
        ("nameid", ColNameId),
        ("Odpracováno měsíců", ColOdpracovanychMesicu),
        ("Počet měsíců", ColOdpracovanychMesicu),
        ("Plat", ColPlat),
        ("Odměna/příjem", ColPlat),
        ("Odměny", ColOdmeny),
        ("Mimořádná odměna", ColOdmeny),
        ("Další příspěvky", ColPrispevky),
        ("Nefinanční bonus", ColNefinancniBonusy),
        ("Poznámka", ColPoznamka),
        ("Funkce", ColFunkce),
        ("Uvolněný", ColUvolneny),
        ("Náhrady na reprezentaci", ColRepreNahrady),
        ("Náhrady cestovní", ColCestoNahrady),
        ("Náhrady na kancelář", ColKanclNahrady),
        ("Náhrady na ubytování", ColUbytovaciNahrady),
        ("Náhrady na administrativu", ColAdministrativniNahrady),
        ("Náhrady na asistenta", ColAsistentNahrady),
        ("Náhrady na telefon", ColTelefonNahrady),
    ];

    public ParsePoliticiResult Parse(Stream stream)
    {
        ResetState();

        using var package = new ExcelPackage(stream);
        Sheet = package.Workbook.Worksheets[0];

        var (instituce, ico, ds, rok) = ParseHeader();
        _rok = rok;
        DetectColumns();
        // if(ColumnMap.Count < )
        
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
        try
        {
            var nazevFunkce = GetCellText(ColFunkce);
            
            var nameId = GetCellText(ColNameId).ToLower().Trim();
            
            var platString = Utils.TrimBadChars(GetCellText(ColPlat));
            decimal? plat = null;
            if (!string.IsNullOrWhiteSpace(platString))
            {
                plat = HlidacStatu.Util.TextTools.GetDecimalFromText(platString);
            }

            // Prázndný řádek
            if (string.IsNullOrWhiteSpace(nazevFunkce) && string.IsNullOrWhiteSpace(nameId) && string.IsNullOrWhiteSpace(platString))
            {
                return null;
            }
            
            if (string.IsNullOrWhiteSpace(nameId))
            {
                throw new InvalidOperationException($"Chybí {nameof(nameId)} na řádku {CurrentRow}."); 
            }
            
            if (string.IsNullOrWhiteSpace(nazevFunkce))
            {
                throw new InvalidOperationException($"Chybí {nameof(nazevFunkce)} na řádku {CurrentRow}."); 
            }

            var uvolnenyString = GetCellText(ColUvolneny)?.Trim().RemoveDiacritics().ToLower();
            int? uvolneny = uvolnenyString switch
            {
                var s when string.IsNullOrWhiteSpace(s) => null,
                var s when s.StartsWith("uv", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.StartsWith("ne", StringComparison.OrdinalIgnoreCase) => 0,
                _ => null
            };

            var odpracovanychMesicuString = Utils.TrimBadChars(GetCellText(ColOdpracovanychMesicu));
            if (!decimal.TryParse(odpracovanychMesicuString, out var odpracovanychMesicu))
            {
                throw new InvalidOperationException($"Chybí počet odpracovaných měsíců na řádku {CurrentRow}.");
            }

            var odmenyString = Utils.TrimBadChars(GetCellText(ColOdmeny));
            decimal? odmeny = null;
            if (!string.IsNullOrWhiteSpace(odmenyString))
            {
                odmeny = HlidacStatu.Util.TextTools.GetDecimalFromText(odmenyString);
            }

            var prispevkyString = Utils.TrimBadChars(GetCellText(ColPrispevky));
            decimal? prispevky = HlidacStatu.Util.TextTools.GetDecimalFromText(prispevkyString);

            var repreNahradyString = Utils.TrimBadChars(GetCellText(ColRepreNahrady));
            decimal? repreNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(repreNahradyString);

            var cestoNahradyString = Utils.TrimBadChars(GetCellText(ColCestoNahrady));
            decimal? cestoNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(cestoNahradyString);

            var kanclNahradyString = Utils.TrimBadChars(GetCellText(ColKanclNahrady));
            decimal? kanclNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(kanclNahradyString);

            var ubytovaciNahradyString = Utils.TrimBadChars(GetCellText(ColUbytovaciNahrady));
            decimal? ubytovaciNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(ubytovaciNahradyString);

            var administrativniNahradyString = Utils.TrimBadChars(GetCellText(ColAdministrativniNahrady));
            decimal? administrativniNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(administrativniNahradyString);

            var asistentNahradyString = Utils.TrimBadChars(GetCellText(ColAsistentNahrady));
            decimal? asistentNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(asistentNahradyString);

            var telefonNahradyString = Utils.TrimBadChars(GetCellText(ColTelefonNahrady));
            decimal? telefonNahrady = HlidacStatu.Util.TextTools.GetDecimalFromText(telefonNahradyString);

            PpPrijem politikPrijem = new()
            {
                NazevFunkce = nazevFunkce,
                Nameid = nameId.ToLower(),
                Rok = _rok,
                Uvolneny = uvolneny,
                PocetMesicu = odpracovanychMesicu,
                Plat = plat,
                Odmeny = odmeny,
                Prispevky = prispevky,
                NahradaAdministrativa = administrativniNahrady,
                NahradaAsistent = asistentNahrady,
                NahradaCestovni = cestoNahrady,
                NahradaKancelar = kanclNahrady,
                NahradaReprezentace = repreNahrady,
                NahradaTelefon = telefonNahrady,
                NahradaUbytovani = ubytovaciNahrady,
                NefinancniBonus = GetCellText(ColNefinancniBonusy),
                PoznamkaPlat = GetCellText(ColPoznamka),
                Status = PpPrijem.StatusPlatu.PotvrzenyPlat_od_organizace
            };

            return politikPrijem;
        }
        catch (Exception ex)
        {
            Warnings.Add($"neočekávaná chyba na řádku {CurrentRow}: {ex.Message}");
            return null;
        }
    }
}
