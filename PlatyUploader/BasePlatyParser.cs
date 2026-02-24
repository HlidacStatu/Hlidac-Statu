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
        string? instituce = null;
        string? ico = null;
        string? ds = null;
        int? rok = null;
        int lastHeaderRow = HeaderSearchStartRow;

        for (var row = HeaderSearchStartRow; row <= HeaderSearchEndRow; row++)
        {
            for (var col = HeaderSearchFirstColumn; col <= HeaderSearchLastColumn; col++)
            {
                var text = Sheet.Cells[$"{col}{row}"].Text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (text.StartsWith("Instituce", StringComparison.InvariantCultureIgnoreCase))
                {
                    instituce = FindValueToRight(row, col)
                                ?? throw new InvalidOperationException("Instituce nemá vyplněnou hodnotu.");
                    if (row > lastHeaderRow) lastHeaderRow = row;
                    break;
                }

                if (text.StartsWith("IČ", StringComparison.InvariantCultureIgnoreCase)
                    || text.StartsWith("ICO", StringComparison.InvariantCultureIgnoreCase))
                {
                    ico = FindValueToRight(row, col)
                          ?? throw new InvalidOperationException("IČO nemá vyplněnou hodnotu.");
                    if (row > lastHeaderRow) lastHeaderRow = row;
                    break;
                }

                if (text.StartsWith("Datová schránka", StringComparison.InvariantCultureIgnoreCase)
                    || text.Equals("DS", StringComparison.InvariantCultureIgnoreCase))
                {
                    ds = FindValueToRight(row, col)
                         ?? throw new InvalidOperationException("Datová schránka nemá vyplněnou hodnotu.");
                    if (row > lastHeaderRow) lastHeaderRow = row;
                    break;
                }

                if (text.StartsWith("Za rok", StringComparison.InvariantCultureIgnoreCase)
                    || text.Equals("Rok", StringComparison.InvariantCultureIgnoreCase))
                {
                    var rokString = FindValueToRight(row, col);
                    rok = Utils.GetYearFromString(rokString);
                    // Fallback: try extracting year from the label cell itself (e.g. "Za rok 2025")
                    rok ??= Utils.GetYearFromString(text);
                    if (rok is null || rok == 0)
                        throw new InvalidOperationException("Za rok nemá vyplněnou hodnotu.");
                    if (row > lastHeaderRow) lastHeaderRow = row;
                    break;
                }
            }
        }

        CurrentRow = lastHeaderRow;

        return (instituce, ico, ds, rok.Value);
    }

    private string? FindValueToRight(int row, char labelCol, int maxGap = 3)
    {
        for (var offset = 1; offset <= maxGap + 1; offset++)
        {
            var col = (char)(labelCol + offset);
            if (col > 'Z')
                break;

            var text = Sheet.Cells[$"{col}{row}"].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
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
