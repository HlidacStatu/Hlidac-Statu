using System;
using System.Linq;
using HlidacStatu.DetectJobs;

namespace HlidacStatu.JobTableEditor.Data
{
    public class CellShell
    {
        public InHtmlTables.Cell Cell { get; private set; }

        public string Id { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }

        public InHtmlTables.Cell.GuessedCellType CellType { get; set; }
        public string Value { get; set; }

        public string Error { get; set; }

        public CellShell(InHtmlTables.Cell cell, int row, int column)
        {
            Row = row;
            Column = column;
            Cell = cell;
            Id = $"[{Row}][{Column}]";
            CellType = cell.CellType;
            SetMergedValue(); //pre-set Value to be displayed
        }

        public void RotateType()
        {
            var allEnums = Enum.GetValues<InHtmlTables.Cell.GuessedCellType>();
            int nextPosition = Array.IndexOf(allEnums, CellType) + 1;
            CellType = nextPosition == allEnums.Length ? allEnums[0] : allEnums[nextPosition];
        }

        public void SetMergedValue()
        {
            Value = CellType switch
            {
                InHtmlTables.Cell.GuessedCellType.Position => string.Join(' ', Cell.FoundKeywords.Keys),
                InHtmlTables.Cell.GuessedCellType.Price or
                    InHtmlTables.Cell.GuessedCellType.PriceWithVAT => Cell.FoundNumbers.FirstOrDefault().ToString("N2"),
                _ => Cell.Text
            };
        }

        public void SetOriginalValue()
        {
            Value = Cell.Text;
        }
    }
}