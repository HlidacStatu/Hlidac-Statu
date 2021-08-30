using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.JobTableEditor.Data
{
    public class JobService
    {
        public async Task<SomeTable> GetNewTable(string user, CancellationToken cancellationToken)
        {
            //Get Data from queue
            // Smlouva item = SmlouvaRepo.Load("13894880", includePrilohy: false);
            // Smlouva.Priloha pril = item.Prilohy.First(m => m.hash.Value == "6411d84d9c37b3691c7f8cdc6017989920f5e66d483cf335589b408e1c7a88cb");
            // var tbls = SmlouvaPrilohaExtension.GetTablesFromPriloha(item, pril, false);
            // //zajima te pouze Strana 7, 2 tabulka na stránce, algorithm stream
            // var tbl = tbls.First(m => m.Algorithm == "stream").Tables.First(t => t.Page == 7 && t.TableInPage == 2);
            // var score = InHtmlTables.TableWithWordsAndNumbers(tbl.ParsedContent(),
            //     InHtmlTables.SpecificWords,
            //     out var foundJobs,
            //     out var cells);

            var table = await InDocTablesRepo.GetNextForCheck(user, cancellationToken);
            InHtmlTables.TableWithWordsAndNumbers(table.ParsedContent(),
                InHtmlTables.SpecificWords,
                out var foundJobs,
                out var cells);
            
            var st = new SomeTable
            {
                Author = user,
                Cells = WrapCells(cells),
                InDocTable = table
            };

            return st;
        }

        private static CellShell[][] WrapCells(InHtmlTables.Cell[][] cells)
        {
            CellShell[][] cellShells = new CellShell[cells.Length][];
            for (int row = 0; row < cells.Length; row++)
            {
                cellShells[row] = new CellShell[cells[row].Length];
                for (int col = 0; col < cells[row].Length; col++)
                {
                    cellShells[row][col] = new CellShell(cells[row][col], row, col);
                }
            }

            return cellShells;
        }

        public async Task SaveChanges(SomeTable table, InDocTables.CheckStatuses operation)
        {
            // push changes to server
            try
            {
                var parsedJobs = table.ParseJobs();
                await InDocJobsRepo.Save(parsedJobs);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            await InDocTablesRepo.ChangeStatus(table.InDocTable,
                operation,
                table.Author,
                (long) table.ProcessingTime.TotalMilliseconds);
        }
        
    }
    

    // assembled object from loaded data and some meta data which are going to be pushed on server
    public class SomeTable
    {
        public InDocTables InDocTable { get; set; }
        public CellShell[][] Cells { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string Author { get; set; }

        public List<InDocJobs> ParseJobs()
        {
            var jobsList = new List<InDocJobs>();
            InDocJobs foundJob = null;
            foreach (var row in Cells)
            {
                foreach (var cell in row)
                {
                    switch (cell.CellType)
                    {
                        case InHtmlTables.Cell.GuessedCellType.Position:
                            if (foundJob != null)
                                jobsList.Add(foundJob);
                            
                            foundJob = new InDocJobs
                            {
                                JobRaw = cell.Value,
                                TablePk = InDocTable.Pk
                            };
                            break;
                        case InHtmlTables.Cell.GuessedCellType.Price:
                            if (foundJob == null)
                            {
                                cell.Error = "Cena nemuze byt drive nez pozice!";
                                throw new Exception(cell.Error);
                            }
                            else
                            {
                                foundJob.SalaryMD = Devmasters.ParseText.ToDecimal(cell.Value);
                            }
                            break;
                        case InHtmlTables.Cell.GuessedCellType.PriceWithVAT:
                            if (foundJob == null)
                            {
                                cell.Error = "Cena nemuze byt drive nez pozice!";
                                throw new Exception(cell.Error);
                            }
                            else
                            {
                                foundJob.SalaryMdVAT = Devmasters.ParseText.ToDecimal(cell.Value);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            if (foundJob != null)
                jobsList.Add(foundJob);

            return jobsList;
        }

    }

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