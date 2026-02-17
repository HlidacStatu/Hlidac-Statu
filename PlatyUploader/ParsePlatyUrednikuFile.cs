using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Serilog;
using OfficeOpenXml;

namespace PlatyUploader;

public class ParsePlatyUrednikuFile
{
    private static readonly ILogger _logger = Log.ForContext<ParsePlatyUrednikuFile>();

    public static async Task HandleFileUploadAsync(string filePath, DateTime denOdeslaniDatovky, DateTime denPrijetiOdpovedi)
    {
        try
        {
            Console.Write(filePath);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.Write("ERROR: FILE NOT FOUND");
            }

            var fileStream = File.OpenRead(filePath);
            await ParseExcelWithPlatyAsync(fileStream, denOdeslaniDatovky, denPrijetiOdpovedi);
            
            Console.Write($"=> DONE");
        }
        catch (Exception e)
        {
            _logger.Warning(e, $"Při uploadu došlo k chybě");
            Console.Write($"=> ERROR: {e.Message}");
        }

        Console.Write(Environment.NewLine);
    }

    
    private static async Task ParseExcelWithPlatyAsync(Stream stream, DateTime denOdeslaniDatovky, DateTime denPrijetiOdpovedi)
    {
        using var package = new ExcelPackage(stream);
        var sheet = package.Workbook.Worksheets[0];

        var excelData = new PlatyUrednikuParsedData(sheet, denOdeslaniDatovky, denPrijetiOdpovedi);
        excelData.ParseHeader();
        excelData.DetectColumns();

        excelData.ParseAllPlaty();

        excelData.PresaveChecksAndFixes();
        await excelData.SaveDataAsync();
    }
    


    public class PlatyUrednikuParsedData
    {
        private ExcelWorksheet _sheet;
        private Dictionary<ColumnName, char> ColumnMap { get; set; } = new();
        private int CurrentRow { get; set; }

        // metadata
        private readonly DateTime _denOdeslaniDatovky;
        private readonly DateTime _denPrijetiOdpovedi;

        // header
        private string? Instituce { get; set; }
        private int? Rok { get; set; }
        private string? Ico { get; set; }
        private string? Ds { get; set; }
        
        private int IdOrganizace { get; set; }

        //body
        private List<PuPlat> Platy { get; set; } = new();


        public PlatyUrednikuParsedData(ExcelWorksheet sheet, DateTime denOdeslaniDatovky, DateTime denPrijetiOdpovedi)
        {
            _sheet = sheet;
            _denOdeslaniDatovky = denOdeslaniDatovky;
            _denPrijetiOdpovedi = denPrijetiOdpovedi;
        }


        public void ParseHeader()
        {
            char? headerColumn = null;
            int? headerRow = null;
            for (int row = 2; row <= 8; row++)
            {
                for (char col = 'A'; col <= 'F'; col++)
                {
                    var text = _sheet.Cells[$"{col}{row}"].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(text) ||
                        text.Equals("Instituce", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    headerColumn = col;
                    headerRow = row;
                    break;
                }
            }

            if (headerColumn is null || headerRow is null)
            {
                throw new Exception("Hlavička nenalezena");
            }

            Instituce = _sheet.Cells[$"{headerColumn}{headerRow}"].Text?.Trim();
            Ico = _sheet.Cells[$"{headerColumn}{headerRow + 1}"].Text?.Trim();
            Ds = _sheet.Cells[$"{headerColumn}{headerRow + 2}"].Text?.Trim();
            var rokString = _sheet.Cells[$"{headerColumn}{headerRow + 3}"].Text;
            Rok = Utils.GetYearFromString(rokString);

            CurrentRow = headerRow.Value + 3;
        }

        enum ColumnName
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

        public void DetectColumns()
        {
            var patterns = new (string Pattern, ColumnName Column)[]
            {
                ("Pozice", ColumnName.Pozice),
                ("Rok", ColumnName.Rok),
                ("Odpracováno", ColumnName.OdpracovanychMesicu),
                ("Výše úvazku", ColumnName.Uvazek),
                ("Plat", ColumnName.Plat),
                ("Odměny", ColumnName.Odmeny),
                ("Nefinanční", ColumnName.NefinancniBonus),
                ("Poznámka", ColumnName.Poznamka),
            };


            for (var attempt = 0; attempt < 20; attempt++)
            {
                CurrentRow++;

                for (char c = 'A'; c <= 'Z'; c++)
                {
                    string? cellText = _sheet.Cells[$"{c}{CurrentRow}"].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(cellText))
                        continue;

                    foreach (var (pattern, column) in patterns)
                    {
                        if (cellText.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                        {
                            ColumnMap.TryAdd(column, c);
                            break;
                        }
                    }
                }

                if (ColumnMap.Count > 0)
                    return;
            }

            throw new Exception("Nenalezeny hlavičky sloupců s daty.");
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
                    Console.Write($" - chybí rok pro řádek {CurrentRow}");
                }

                var odpracovanychMesicuString = Utils.TrimBadChars(GetCellText(ColumnName.OdpracovanychMesicu));
                if (!decimal.TryParse(odpracovanychMesicuString, out var odpracovanychMesicu))
                {
                    Console.Write($" - chybí odpracované měsíce pro řádek {CurrentRow}");
                }

                var uvazekString = Utils.TrimBadChars(GetCellText(ColumnName.Uvazek));
                if (!decimal.TryParse(uvazekString, out var uvazek))
                {
                    Console.Write($" - chybí úvazek pro řádek {CurrentRow}");
                }

                var hrubyPlatString = Utils.TrimBadChars(GetCellText(ColumnName.Plat));
                decimal? hrubyPlat = null;
                if (!string.IsNullOrWhiteSpace(hrubyPlatString))
                {
                    hrubyPlat = HlidacStatu.Util.TextTools.GetDecimalFromText(hrubyPlatString);
                }

                if (hrubyPlat is null)
                {
                    Console.Write($" - neplatný hrubý plat pro řádek {CurrentRow}");
                }

                var odmenyString = Utils.TrimBadChars(GetCellText(ColumnName.Odmeny));
                decimal? odmeny = null;
                if (!string.IsNullOrWhiteSpace(odmenyString))
                {
                    odmeny = HlidacStatu.Util.TextTools.GetDecimalFromText(odmenyString);
                }

                if (odmeny is null)
                {
                    Console.Write($" - neplatné odměny rok pro řádek {CurrentRow}");
                }

                PuPlat plat = new()
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

                return plat;
            }
            catch
            {
                return null;
            }
        }

        private string GetCellText(ColumnName columnName)
        {
            if (ColumnMap.TryGetValue(columnName, out var column))
            {
                return _sheet.Cells[$"{column}{CurrentRow}"].Text.Trim();
            }

            return String.Empty;
        }

        public void ParseAllPlaty()
        {
            int maxEmptySubsequentRows = 10;

            while (true)
            {
                CurrentRow++;
                PuPlat? plat = ParsePlatOnCurrentRow();

                if (plat is null)
                {
                    if (maxEmptySubsequentRows-- <= 0)
                    {
                        break;
                    }

                    continue;
                }
                
                Platy.Add(plat);
                maxEmptySubsequentRows = 10;
            }
        }


        public void PresaveChecksAndFixes()
        {
            if (string.IsNullOrWhiteSpace(Ds))
            {
                throw new($"V souboru není uvedená datová schránka.");
            }

            if (Rok is null)
            {
                throw new($"Rok nenalezen v hlavičce.");
            }

            if (Platy.Any(p => p.Rok != Rok))
            {
                throw new($"Rok hlavičky se neshoduje s rokem platu.");
            }

            if (Platy.Any(p => p.PocetMesicu < 0 || p.PocetMesicu > 12))
            {
                throw new($"Nesmyslný počet odpracovaných měsíců.");
            }

            var avgPlat = Platy.Average(p => p.HrubyMesicniPlatVcetneOdmen);
            if (avgPlat <= 30_000 || avgPlat >= 350_000)
            {
                throw new($"Průměrný plat je mimo range <30 000 a 350 000>.");
            }

            //set unique name for nazev pozice
            HashSet<string> uniqueNames = new HashSet<string>();
            foreach (var plat in Platy)
            {
                int counter = 0;
                string newName = plat.NazevPozice;
                
                while (uniqueNames.Contains(newName))
                {
                    newName = $"{plat.NazevPozice} {counter++}";
                }
                uniqueNames.Add(newName);
                plat.NazevPozice = newName;
            }
        }

        public async Task SaveDataAsync()
        {
            IdOrganizace = await LoadOrCreateOrganizaceAsync(Ds!); //Ds kontrolujeme v PresaveChecksAndFixes

            bool zduvodneniOdmen = false;
            
            foreach (var puPlat in Platy)
            {
                puPlat.IdOrganizace = IdOrganizace;
                await PuRepo.UpsertPlatAsync(puPlat);

                if (zduvodneniOdmen == false && puPlat.Odmeny > 0 && !string.IsNullOrWhiteSpace(puPlat.PoznamkaPlat))
                {
                    zduvodneniOdmen = true;
                }
            }
            
            //save metadata
            var metadata = new PuOrganizaceMetadata()
            {
                IdOrganizace =  IdOrganizace,
                ZduvodneniMimoradnychOdmen = zduvodneniOdmen,
                DatumOdeslaniZadosti = _denOdeslaniDatovky,
                DatumPrijetiOdpovedi = _denPrijetiOdpovedi,
                Rok = Rok!.Value, //rok kontrolujeme v PresaveChecksAndFixes
                Typ = PuOrganizaceMetadata.TypMetadat.PlatyUredniku,

            };
            
            await PuRepo.UpsertMetadataAsync(metadata);
        }
        
        private async Task<int> LoadOrCreateOrganizaceAsync(string? ds)
        {
            if(string.IsNullOrWhiteSpace(ds))
                throw new ArgumentNullException(nameof(ds));
        
            var origOrganizace = await PuRepo.GetFullDetailAsync(ds);
            int idOrganizace = origOrganizace?.Id ?? 0;
            if (idOrganizace == 0)
            {
                var newOrganizace = new PuOrganizace()
                {
                    DS = ds
                };
                await PuRepo.UpsertOrganizaceAsync(newOrganizace);

                idOrganizace = newOrganizace.Id;

                Console.Write(" - vytvořena nová organizace");
            }

            return idOrganizace;
        }
    }
}