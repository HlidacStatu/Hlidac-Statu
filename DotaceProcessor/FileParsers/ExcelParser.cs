using OfficeOpenXml;
using Serilog;

namespace DotaceProcessor.FileParsers;

public class ExcelParser : IFileParser
{
    private ILogger Logger = Log.ForContext(typeof(ExcelParser));

    private List<string> Headers { get; set; } = new();
    
    public IEnumerable<IDictionary<string, object?>> Parse(Stream fileStream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus requires setting the license context

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) yield break;

        var headers = IdentifyHeaderRow(worksheet, out int headerRowIndex);
        if (headers == null) yield break;
        
        Headers = headers;

        for (int row = headerRowIndex + 1; row <= worksheet.Dimension.End.Row; row++)
        {
            if (IsEmptyRow(worksheet, row)) continue; // Skip empty rows

            if (ContainsSummation(worksheet, row)) continue; // Skip rows that may contain summation

            var rowData = new Dictionary<string, object?>();
            for (int col = worksheet.Dimension.Start.Column; col <= Headers.Count; col++)
            {
                var header = Headers[col - 1]; // List is zero-based, but Excel columns are one-based
                var cellValue = worksheet.Cells[row, col].Text;

                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    rowData[header] = cellValue;
                }
                else
                {
                    rowData[header] = null; // Handle empty cells as null
                }
            }

            yield return rowData;
        }
    }

    private bool IsEmptyRow(ExcelWorksheet worksheet, int row)
    {
        for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
        {
            if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
            {
                return false;
            }
        }
        return true;
    }

    private bool ContainsSummation(ExcelWorksheet worksheet, int row)
    {
        int dataCellCount = 0;

        for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
        {
            var cell = worksheet.Cells[row, col];
            var cellValue = cell.Text.Trim();

            if (!string.IsNullOrWhiteSpace(cellValue))
            {
                dataCellCount++;
            }

            // Check if the cell contains a formula with SUM()
            if (cell.Formula?.ToLower().Contains("sum(") == true)
            {
                Logger.Warning($"Row {row} contains a summation formula.");
                return true;
            }
        }

        // Calculate the percentage of cells with data in the current row
        double percentageWithData = (double)dataCellCount / Headers.Count;

        // Log a warning and return true if less than 30% of cells contain data compared to the average
        if (percentageWithData <= 0.25)
        {
            Logger.Warning($"Row {row} has less than 30% of cells with data compared to the average.");
            return true;
        }

        return false;
    }

    private List<string>? IdentifyHeaderRow(ExcelWorksheet worksheet, out int headerRowIndex)
    {
        for (int row = worksheet.Dimension.Start.Row; row <= worksheet.Dimension.End.Row; row++)
        {
            if (IsPotentialHeaderRow(worksheet, row))
            {
                var headers = new List<string>();
                for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Text;
                    headers.Add(!string.IsNullOrWhiteSpace(cellValue) ? cellValue : "empty header");
                }

                if (headers.Count > 0)
                {
                    headerRowIndex = row;
                    return headers;
                }
            }
        }

        headerRowIndex = -1; // No header row found
        return null;
    }

    private bool IsPotentialHeaderRow(ExcelWorksheet worksheet, int row)
    {
        int score = 0;
        for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
        {
            string cellText = worksheet.Cells[row, col].Text;
            if (string.IsNullOrWhiteSpace(cellText))
            {
                continue;
            }

            score += ScoreHeaderMarkers(cellText);
        }

        // Počítám, že by mělo být v maximálně minimalistické verzi alespoň 6 sloupců
        // a alespoň 3 z nich by měly být bodované (3x3 = 9)
        // celkem cca 15 - 16 bodů?
        return score >= 16;
    }

    private int ScoreHeaderMarkers(string text) =>
        text.Trim() switch
        {
            _ when text.StartsWith("název projektu", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("požadovaná dotace", StringComparison.InvariantCultureIgnoreCase) => 5,
            _ when text.StartsWith("schválená dotace", StringComparison.InvariantCultureIgnoreCase) => 5,
            _ when text.StartsWith("žadatel název", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("žadatel ičo", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("název žadatel", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("ičo žadatel", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("žadatel obec", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("žadatel_obec", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("cislo_projektu", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("nazev_projektu", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("popis", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("stav", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("nazev_programu", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("typ_dotace", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("nazev_oblasti", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("ucel_dotace", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("rok_od", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("rok_do", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("castka_naklady", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("castka_pozadovana", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("castka_pridelena", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ when text.StartsWith("ič", StringComparison.InvariantCultureIgnoreCase) => 2,
            _ when text.StartsWith("datum", StringComparison.InvariantCultureIgnoreCase) => 2,
            _ when text.StartsWith("kód", StringComparison.InvariantCultureIgnoreCase) => 2,
            _ when text.StartsWith("adresa", StringComparison.InvariantCultureIgnoreCase) => 3,
            _ when text.StartsWith("žadatel", StringComparison.InvariantCultureIgnoreCase) => 3,
            _ when text.EndsWith("částka", StringComparison.InvariantCultureIgnoreCase) => 4,
            _ => 1 //nonempty column has score as well
        };
}