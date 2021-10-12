using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.JobTableEditor.Data
{
    public class JobService
    {
        static readonly InTables it_inTables = new("IT", IT.Keywords, IT.OtherWords, IT.BlacklistedWords);


        public async Task<SomeTable> GetNewTable(string user, CancellationToken cancellationToken)
        {
            //todo: až bude víc oborů, tak to tady rozšířit, aby se načítal konkrétní obor
            // nejprve v tabulce a poté se použil správný parser
            var table = await InDocTablesRepo.GetNextForCheck(user, cancellationToken);
            it_inTables.TableWithWordsAndNumbers(table.ParsedContent(),
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

        public async Task<SomeTable> GetSpecificTable(int pk, string user, CancellationToken cancellationToken)
        {
            var table = await InDocTablesRepo.GetSpecific(pk, user, cancellationToken);
            var score = it_inTables.TableWithWordsAndNumbers(table.ParsedContent(),
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

        public async Task SaveChanges(SomeTable table, InDocTables.CheckStatuses operation)
        {
            // remove old values
            await InDocJobsRepo.Remove(table.InDocTable.Pk);
            // push changes to server
            if (operation != InDocTables.CheckStatuses.WrongTable
                && operation != InDocTables.CheckStatuses.ForNextReview
            )
            {
                try
                {
                    var parsedJobs = table.ParseJobs();

                    await InDocJobsRepo.SaveAsync(parsedJobs);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            await InDocTablesRepo.ChangeStatus(table.InDocTable,
                operation,
                table.Author,
                (long)table.ProcessingTime.TotalMilliseconds);
        }

        public async Task<string> GetRandomStatistic(string user, CancellationToken cancellationToken)
        {
            var globalStatistic = InDocTablesRepo.GlobalStatistic(cancellationToken);
            var userStatistic = InDocTablesRepo.UserStatistic(user, cancellationToken);
            var currentSecond = DateTime.Now.Second;
            var statistiky = new List<string>();
            int number = 0;
            await Task.WhenAll(globalStatistic, userStatistic);
            
            //global
            if (globalStatistic.Result.TryGetValue(InDocTables.CheckStatuses.Done, out number))
            {
                statistiky.Add($"Víš, že už je zpracovaných celkem {number} tabulek?");
            }
            if (globalStatistic.Result.TryGetValue(InDocTables.CheckStatuses.WrongTable, out number))
            {
                statistiky.Add($"Víš, že už bylo označeno celkem {number} tabulek jako 💩?");
            }
            if (globalStatistic.Result.TryGetValue(InDocTables.CheckStatuses.ForNextReview, out number))
            {
                statistiky.Add($"Víš, že na review čeká Michala a Petra {number} kousků?");
            }
            if (globalStatistic.Result.TryGetValue(InDocTables.CheckStatuses.WaitingInQueue, out number))
            {
                statistiky.Add($"Víš, že aby bylo hotovo úplně všechno, musí se zpracovat ještě {number} tabulek?");
            }
            
            //user
            if (userStatistic.Result.TryGetValue(InDocTables.CheckStatuses.Done, out number))
            {
                statistiky.Add($"Že si nepočítáš, kolik jsi už jsi toho zvládla? Já jo a už jsi zpracovala {number} tabulek?");
            }
            if (userStatistic.Result.TryGetValue(InDocTables.CheckStatuses.WrongTable, out number))
            {
                statistiky.Add($"To jsi věděla, že celkem jsi označila {number} tabulek jako nepoužitelných?");
            }
            if (userStatistic.Result.TryGetValue(InDocTables.CheckStatuses.ForNextReview, out number))
            {
                statistiky.Add($"Na review už jsi poslala {number} divnotabulek. Petr s Michalem Ti mockrát \"děkují\".");
            }


            return statistiky[currentSecond % statistiky.Count];


        }
    }


    // assembled object from loaded data and some meta data which are going to be pushed on server
}