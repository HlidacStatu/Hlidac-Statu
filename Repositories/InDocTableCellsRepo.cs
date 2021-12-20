using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using System;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class InDocTableCellsRepo
    {
 
        public static InDocTableCells Add(InDocTableCells cells)
        {
            IndexResponse res; 
            try
            {
                res = Manager.GetESClient_InDocTableCells()
                    .Index<InDocTableCells>(cells, m => m.Id(cells.Id));
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