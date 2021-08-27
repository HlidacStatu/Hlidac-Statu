using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

namespace HlidacStatu.JobTableEditor.Data
{
    public class JobService
    {
        public async Task<SomeTable> GetNewTable()
        {
            //Get Data from queue
            Smlouva item = SmlouvaRepo.Load("13894880", includePrilohy: false);
            Smlouva.Priloha pril = item.Prilohy.First(m => m.hash.Value == "6411d84d9c37b3691c7f8cdc6017989920f5e66d483cf335589b408e1c7a88cb");
            var tbls = SmlouvaPrilohaExtension.GetTablesFromPriloha(item, pril, false);
            //zajima te pouze Strana 7, 2 tabulka na stránce, algorithm stream
            var tbl = tbls.First(m => m.Algorithm == "stream").Tables.First(t => t.Page == 7 && t.TableInPage == 2);
            var score = InHtmlTables.TableWithWordsAndNumbers(tbl.ParsedContent(),
                InHtmlTables.SpecificWords,
                out var foundJobs,
                out var cells);
            
            await Task.Delay(200);

            var st = new SomeTable();

            CellShell[][] cellShells = new CellShell[cells.Length][];
            for (int row = 0; row < cells.Length; row++)
            {
                cellShells[row] = new CellShell[cells[row].Length];
                for (int col = 0; col < cells[row].Length; col++)
                {
                    cellShells[row][col] = new CellShell(cells[row][col], row, col);
                }
            }

            st.Cells = cellShells;

            return st;
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
        public TimeSpan ProcessingTime { get; set; }
        public string Author { get; set; }
        
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