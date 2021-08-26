using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.DetectJobs;

namespace HlidacStatu.JobTableEditor.Data
{
    public class JobService
    {
        public async Task<SomeTable> GetNewTable()
        {
            //Get Data from queue
            
            
            await Task.Delay(200);
            return new SomeTable();
        }

        public async Task SaveChanges(SomeTable table)
        {
            // push changes to server
            await Task.Delay(200);
        }
    }

    // assembled object from loaded data and some meta data which are going to be pushed on server
    public class SomeTable
    {
        public CellShell[][] Cells { get; set; }
    }

    public class CellShell
    {
        public InHtmlTables.Cell Cell { get; private set; }

        public string Id { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }
        
        public InHtmlTables.Cell.GuessedCellType CellType { get; set; }
        public string Value { get; set; }

        public CellShell(InHtmlTables.Cell cell, int row, int column)
        {
            Row = row;
            Column = column;
            Cell = cell;
            Id = $"[{Row}][{Column}]";

            CellType = cell.CellType;
            Value = cell.CellType switch
            {
                InHtmlTables.Cell.GuessedCellType.Position => string.Join(' ', cell.FoundKeywords.Keys),
                InHtmlTables.Cell.GuessedCellType.Price or
                    InHtmlTables.Cell.GuessedCellType.PriceWithVAT => cell.FoundNumbers.FirstOrDefault().ToString("N"),
                _ => cell.Text
            };
        }

        public void RotateType()
        {
            var allEnums = Enum.GetValues<InHtmlTables.Cell.GuessedCellType>();
            int nextPosition = Array.IndexOf(allEnums, CellType) + 1;
            CellType = nextPosition == allEnums.Length ? allEnums[0] : allEnums[nextPosition];
        }
    }
    
}