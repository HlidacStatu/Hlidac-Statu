using OfficeOpenXml;

namespace PlatyUploader;

public abstract class BasePlatyParser<TPlat> where TPlat : class
{
    // Header search boundaries
    protected const int HeaderSearchStartRow = 2;
    protected const int HeaderSearchEndRow = 8;
    protected const char HeaderSearchFirstColumn = 'A';
    protected const char HeaderSearchLastColumn = 'F';

    // Column detection
    protected const int MaxColumnDetectionAttempts = 20;
    protected const char DataFirstColumn = 'A';
    protected const char DataLastColumn = 'Z';

    // Row parsing
    protected const int MaxConsecutiveEmptyRows = 10;

    protected ExcelWorksheet Sheet = null!;
    protected readonly Dictionary<string, char> ColumnMap = new();
    protected int CurrentRow;
    protected readonly List<string> Warnings = new();

    protected abstract (string Pattern, string ColumnKey)[] GetColumnPatterns();

    protected abstract TPlat? ParsePlatOnCurrentRow();

    protected void ResetState()
    {
        ColumnMap.Clear();
        Warnings.Clear();
    }

    protected (string? instituce, string? ico, string ds, int rok) ParseHeader()
    {
        char? headerColumn = null;
        int? headerRow = null;

        for (var row = HeaderSearchStartRow; row <= HeaderSearchEndRow; row++)
        {
            for (var col = HeaderSearchFirstColumn; col <= HeaderSearchLastColumn; col++)
            {
                var text = Sheet.Cells[$"{col}{row}"].Text?.Trim();
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

        var instituce = Sheet.Cells[$"{headerColumn}{headerRow}"].Text?.Trim();
        var ico = Sheet.Cells[$"{headerColumn}{headerRow + 1}"].Text?.Trim();
        var ds = Sheet.Cells[$"{headerColumn}{headerRow + 2}"].Text?.Trim();
        var rokString = Sheet.Cells[$"{headerColumn}{headerRow + 3}"].Text;
        var rok = Utils.GetYearFromString(rokString);

        if (rok is null || rok == 0)
        {
            throw new InvalidOperationException("Rok nenalezen v hlavičce.");
        }

        if (string.IsNullOrWhiteSpace(ds))
            throw new InvalidOperationException("V souboru není uvedená datová schránka.");

        CurrentRow = headerRow.Value + 3;

        return (instituce, ico, ds, rok.Value);
    }

    protected void DetectColumns()
    {
        var columnPatterns = GetColumnPatterns();

        for (var attempt = 0; attempt < MaxColumnDetectionAttempts; attempt++)
        {
            CurrentRow++;

            for (var col = DataFirstColumn; col <= DataLastColumn; col++)
            {
                var cellText = Sheet.Cells[$"{col}{CurrentRow}"].Text?.Trim();

                if (string.IsNullOrWhiteSpace(cellText))
                    continue;

                foreach (var (pattern, columnKey) in columnPatterns)
                {
                    if (cellText.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ColumnMap.TryAdd(columnKey, col);
                        break;
                    }
                }
            }

            if (ColumnMap.Count > 0)
                return;
        }

        throw new InvalidOperationException("Nenalezeny hlavičky sloupců s daty.");
    }

    protected List<TPlat> ParseAllPlaty()
    {
        var platy = new List<TPlat>();
        var emptyRowStreak = 0;

        while (true)
        {
            CurrentRow++;
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

    protected string GetCellText(string columnKey)
    {
        if (ColumnMap.TryGetValue(columnKey, out var column))
        {
            return Sheet.Cells[$"{column}{CurrentRow}"].Text.Trim();
        }

        return string.Empty;
    }
}
