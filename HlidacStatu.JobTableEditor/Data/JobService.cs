using HlidacStatu.DetectJobs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
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
    }


    // assembled object from loaded data and some meta data which are going to be pushed on server
}