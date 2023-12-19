using HlidacStatu.Entities;
using System;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Nest;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static class InDocTableCellsRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(InDocTableCellsRepo));
 
        public static async Task<InDocTableCells> AddAsync(InDocTableCells cells)
        {
            IndexResponse res; 
            try
            {
                var client = await Manager.GetESClient_InDocTableCellsAsync(); 
                res = await client.IndexAsync<InDocTableCells>(cells, m => m.Id(cells.Id));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Chyba při ukládání cells.");
                return null;
            }

            if (!res.IsValid)
            {
                _logger.Error($"Chyba ukládání cells. {res.DebugInformation}");
            }

            return cells;

        }
    }
}