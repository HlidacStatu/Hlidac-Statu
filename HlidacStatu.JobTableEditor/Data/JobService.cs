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
        static HlidacStatu.DetectJobs.InTables it_inTables = new DetectJobs.InTables(
    HlidacStatu.DetectJobs.IT.Keywords, HlidacStatu.DetectJobs.IT.OtherWords, HlidacStatu.DetectJobs.IT.BlacklistedWords
    );


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
                    await InDocJobsRepo.Save(parsedJobs);
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