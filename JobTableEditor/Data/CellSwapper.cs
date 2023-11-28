using System;
using JobTableEditor.Components;

namespace JobTableEditor.Data
{
    public class CellSwapper
    {
        private Cell CellToSwap;
        
        public void EndSwap(Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            if (CellToSwap is null)
            {
                return;
            }

            if (!ReferenceEquals(CellToSwap, cell))
            {
                (CellToSwap.CellShell.Value, cell.CellShell.Value ) = 
                    (cell.CellShell.Value, CellToSwap.CellShell.Value);
                (CellToSwap.CellShell.CellType, cell.CellShell.CellType) = 
                    (cell.CellShell.CellType, CellToSwap.CellShell.CellType);
                (CellToSwap.CellShell.IsImportant, cell.CellShell.IsImportant) = 
                    (cell.CellShell.IsImportant, CellToSwap.CellShell.IsImportant);
            }
            CellToSwap.SetCellStyle();
            cell.SetCellStyle();
            
            CellToSwap.Redraw(false);
            cell.Redraw(false);

            CellToSwap = null;
        }

        public void StartSwap(Cell cell)
        {
            CellToSwap = cell ?? throw new ArgumentNullException(nameof(cell));
            CellToSwap.Redraw(true);
        }
    }
}