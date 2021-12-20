using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Components;

namespace HlidacStatu.JobTableEditor.Data
{
    public class SomeTable
    {
        public InDocTables InDocTable { get; set; }
        public CellShell[][] Cells { get; set; }
        public TimeSpan ProcessingTime { get; private set; }
        public string Author { get; set; }

        public List<InDocJobs> FoundJobs { get; private set; }
        
        
        private DateTime _tableOpenedAt;
        
        public Func<Task> OnSave { get; set; }
        
        public async Task Save()
        {
            if (OnSave != null)
                await OnSave();
        }
        
        public void StartWork()
        {
            _tableOpenedAt = DateTime.Now;
        }
        
        public void EndWork()
        {
            ProcessingTime = DateTime.Now - _tableOpenedAt;
        }
        
        public void ParseJobs()
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
                            foundJob.Price = Devmasters.ParseText.ToDecimal(cell.Value);
                            foundSomething = true;
                            break;
                        case InTables.Cell.GuessedCellType.PriceWithVAT:
                            foundJob.PriceVAT = Devmasters.ParseText.ToDecimal(cell.Value);
                            foundSomething = true;
                            break;
                        default:
                            break;
                    }
                }

                // existuje název jobu
                bool validJob = !string.IsNullOrWhiteSpace(foundJob.JobRaw);
                if (validJob)
                    jobsList.Add(foundJob);
                else if (foundSomething)
                {
                    throw new Exception($"Na radku [{Array.IndexOf(Cells, row) + 1 }] nebyl nalezen job!");
                }
            }

            FoundJobs = jobsList;
        }
        
        
    }
}