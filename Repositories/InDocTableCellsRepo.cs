using HlidacStatu.Entities;
using System;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class InDocTableCellsRepo
    {
 
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
                HlidacStatu.Util.Consts.Logger.Error("Chyba při ukládání cells.", ex);
                return null;
            }

            if (!res.IsValid)
            {
                HlidacStatu.Util.Consts.Logger.Error($"Chyba ukládání cells. {res.DebugInformation}");
            }

            return cells;

        }
    }
}