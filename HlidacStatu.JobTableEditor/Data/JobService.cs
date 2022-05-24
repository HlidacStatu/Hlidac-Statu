using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HlidacStatu.JobTableEditor.Data
{
    public class JobService
    {
        static readonly InTables it_inTables = new("IT", IT.Keywords, IT.OtherWords, IT.BlacklistedWords);
        
        ILogger<JobService> _logger;

        public JobService(ILogger<JobService> logger)
        {
            _logger = logger;
        }
        
        public async Task<SomeTable> GetNewTable(string obor, string user, CancellationToken cancellationToken)
        {
            //todo: až bude víc oborů, tak to tady rozšířit, aby se načítal konkrétní obor
            // nejprve v tabulce a poté se použil správný parser
            
            //stopwatch
            var sw = new Stopwatch(); 
            sw.Start();
            var table = await InDocTablesRepo.GetNextForCheck(obor, user, cancellationToken);
            sw.Stop();
            var getNextForCheckTime = sw.ElapsedMilliseconds;
            
            sw.Restart();
            var cells = InTables.TableToCells(table.ParsedContent());
            sw.Stop();
            var tableToCellsTime = sw.ElapsedMilliseconds;
            
            sw.Restart();
            var score = it_inTables.CellsWithWordsAndNumbers(cells, out var foundJobs);
            sw.Stop();
            var cellsWithWordsAndNumbersTime = sw.ElapsedMilliseconds;
            
            sw.Restart();
            var st = new SomeTable
            {
                Author = user,
                Cells = WrapCells(cells),
                InDocTable = table
            };
            sw.Stop();
            
            var wrapCellsTime = sw.ElapsedMilliseconds;
            int width = cells.Length > 0 ? cells[0].Length : 0;
            _logger.LogDebug($"GetNewTable loading times - W:{width}, H:{cells.Length} - \nnfc: {getNextForCheckTime}ms \nttc: {tableToCellsTime}ms \ncwwn: {cellsWithWordsAndNumbersTime}ms \nwc: {wrapCellsTime}ms");
            return st;
        }

        public async Task<SomeTable> GetSpecificTable(int pk, string user, CancellationToken cancellationToken)
        {
            var table = await InDocTablesRepo.GetSpecific(pk, user, cancellationToken);
            var cells = InTables.TableToCells(table.ParsedContent());
            var score = it_inTables.CellsWithWordsAndNumbers(cells, out var foundJobs);

            var st = new SomeTable
            {
                Author = user,
                Cells = WrapCells(cells),
                InDocTable = table
            };

            return st;
        }
        
        public async Task<int> WaitingInQueue(string obor, CancellationToken cancellationToken)
        {
            return await InDocTablesRepo.WaitingInQueue(obor, cancellationToken);
        }
        
        public async Task<List<InDocTables>> LoadHistory(string user, int take, CancellationToken cancellationToken)
        {
            var tablesList = await InDocTablesRepo.GetHistory(user, take, cancellationToken);
            
            return tablesList;
        }

        private static CellShell[][] WrapCells(InTables.Cell[][] cells)
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

        public async Task SaveChanges(SomeTable table, InDocTables.CheckState status)
        {
            await InDocJobsRepo.RemoveAsync(table.InDocTable.Pk);

            if (status == InDocTables.CheckState.Done)
            {
                await SaveJobs(table.FoundJobs);
            }
            
            var idt = table.InDocTable;
            idt.CheckStatus = status;
            idt.CheckedBy = table.Author;
            idt.CheckedDate = DateTime.Now;
            idt.CheckElapsedInMs = (int)table.ProcessingTime.TotalMilliseconds;
            
            await InDocTablesRepo.SaveAsync(idt);
            
            if (status == InDocTables.CheckState.Done)
            {
                var cells = new InDocTableCells()
                {
                    Algorithm = table.InDocTable.Algorithm,
                    Page = table.InDocTable.Page,
                    PrilohaHash = table.InDocTable.PrilohaHash,
                    SmlouvaID = table.InDocTable.SmlouvaID,
                    TableOnPage = table.InDocTable.TableOnPage,
                    Date = DateTime.Now,
                    Cells = JsonConvert.SerializeObject(table.Cells)
                };
                await InDocTableCellsRepo.AddAsync(cells);
            }
        }

        private async Task SaveJobs(List<InDocJobs> jobs)
        {
            try
            {
                await InDocJobsRepo.SaveAsync(jobs);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, $"Save jobs failed");
                Console.WriteLine(e);
                throw;
            }
        }

    }


    // assembled object from loaded data and some meta data which are going to be pushed on server
}