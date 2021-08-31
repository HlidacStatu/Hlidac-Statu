using System;
using HlidacStatu.JobTableEditor.Components;

namespace HlidacStatu.JobTableEditor.Data
{
    public class CellSwapper
    {
        private Cell CellToSwap;
        
        public void Swap(Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            if (CellToSwap is null)
            {
                CellToSwap = cell;
                CellToSwap.Redraw(true);
                return;
            }

            if (!ReferenceEquals(CellToSwap, cell))
            {
                var value = CellToSwap.CellShell.Value;
                var cellType = CellToSwap.CellShell.CellType;

                CellToSwap.CellShell.Value = cell.CellShell.Value;
                CellToSwap.CellShell.CellType = cell.CellShell.CellType;

                cell.CellShell.Value = value;
                cell.CellShell.CellType = cellType;
            }
            CellToSwap.SetCellStyle();
            cell.SetCellStyle();
            
            CellToSwap.Redraw(false);
            cell.Redraw(false);

            CellToSwap = null;
        }
    }
}