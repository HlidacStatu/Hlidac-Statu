using System;
using System.Collections.Generic;
using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;

namespace HlidacStatu.JobTableEditor.Data
{
    public class SomeTable
    {
        public InDocTables InDocTable { get; set; }
        public CellShell[][] Cells { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string Author { get; set; }

        public List<InDocJobs> ParseJobs()
        {
            var jobsList = new List<InDocJobs>();
            foreach (var row in Cells)
            {
                InDocJobs foundJob = new InDocJobs();
                bool foundSomething = false;
                foreach (var cell in row)
                {
                    switch (cell.CellType)
                    {
                        case InTables.Cell.GuessedCellType.Position:
                            foundJob.JobRaw = cell.Value;
                            foundJob.TablePk = InDocTable.Pk;
                            foundSomething = true;
                            break;
                        case InTables.Cell.GuessedCellType.Price:
                            foundJob.SalaryMD = Devmasters.ParseText.ToDecimal(cell.Value);
                            foundSomething = true;
                            break;
                        case InTables.Cell.GuessedCellType.PriceWithVAT:
                            foundJob.SalaryMdVAT = Devmasters.ParseText.ToDecimal(cell.Value);
                            foundSomething = true;
                            break;
                        default:
                            break;
                    }
                }

                // existuje název jobu
                // AND má vyplněnou alespoň jednu z cen
                bool validJob = !string.IsNullOrWhiteSpace(foundJob.JobRaw)
                                && (foundJob.SalaryMD.HasValue || foundJob.SalaryMdVAT.HasValue);
                if (validJob)
                    jobsList.Add(foundJob);
                else if (foundSomething)
                {
                    throw new Exception($"Na radku [{Array.IndexOf(Cells, row) + 1 }] neni kompletni job (nazev + cena)!");
                }
            }

            return jobsList;
        }
    }
}