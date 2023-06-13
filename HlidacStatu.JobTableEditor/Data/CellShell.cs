using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HlidacStatu.DetectJobs;
using HlidacStatu.JobTableEditor.Annotations;

namespace HlidacStatu.JobTableEditor.Data
{
    public class CellShell : INotifyPropertyChanged
    {
        private string _value;
        private InTables.Cell.GuessedCellType _cellType;
        private bool _isImportant;

        public InTables.Cell Cell { get; private set; }
        public string Id { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }

        public bool IsImportant
        {
            get => _isImportant;
            set
            {
                if (value == _isImportant) return;
                _isImportant = value;
                OnPropertyChanged(nameof(IsImportant));
            }
        }

        public InTables.Cell.GuessedCellType CellType
        {
            get => _cellType;
            set
            {
                if (value == _cellType) return;
                _cellType = value;
                OnPropertyChanged(nameof(CellType));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        //public string Error { get; set; }
        
        public CellShell(InTables.Cell cell, int row, int column)
        {
            Row = row;
            Column = column;
            Cell = cell;
            Id = $"[{Row}][{Column}]";
            CellType = cell.CellType;
            SetMergedValue(); //pre-set Value to be displayed
        }

        public void SwitchType()
        {
            IsImportant = !IsImportant;
        }

        public void SetMergedValue()
        {
            Value = CellType switch
            {
                InTables.Cell.GuessedCellType.Position => Cell.Text,
                InTables.Cell.GuessedCellType.Price or
                    InTables.Cell.GuessedCellType.PriceWithVAT => Cell.FoundNumbers.FirstOrDefault().ToString("N2"),
                _ => Cell.OriginalCellText
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //RunErrorChecks();
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // public void RunErrorChecks()
        // {
        //     Error = "";
        //     CheckAllowedValue(1000, 20000);
        //     CheckJobIsNotEmpty();
        //
        // }
        //
        // private void CheckJobIsNotEmpty()
        // {
        //     if (CellType == InTables.Cell.GuessedCellType.Position)
        //     {
        //         if (string.IsNullOrWhiteSpace(Value))
        //         {
        //             Error += "Není vyplněn název pozice.";
        //         }
        //     }
        // }
        //
        // private void CheckAllowedValue(decimal from, decimal to)
        // {
        //     decimal vat = 1.21m;
        //     decimal? value = Devmasters.ParseText.ToDecimal(Value);
        //
        //     
        //     if (CellType == InTables.Cell.GuessedCellType.Price)
        //     {
        //         if (!value.HasValue)
        //         {
        //             Error += "Chybí vyplněná hodnota.";
        //             return;
        //         }
        //         
        //         if (value < from)
        //         {
        //             Error += $"Zkontroluj hodnotu, zdá se nám nízká [{value:N1}].";
        //             return;
        //         }
        //         
        //         if (value > to)
        //         {
        //             Error += $"Zkontroluj hodnotu, zdá se nám vysoká [{value:N1}].";
        //             return;
        //         }
        //     }
        //     
        //     if (CellType == InTables.Cell.GuessedCellType.PriceWithVAT)
        //     {
        //         if (!value.HasValue)
        //         {
        //             Error += "Chybí vyplněná hodnota.";
        //             return;
        //         }
        //         
        //         if (value < (from * vat))
        //         {
        //             Error += $"Zkontroluj hodnotu, zdá se nám nízká [{value:N1}].";
        //             return;
        //         }
        //         
        //         if (value > (to * vat))
        //         {
        //             Error += $"Zkontroluj hodnotu, zdá se nám vysoká [{value:N1}].";
        //             return;
        //         }
        //     }
        //     
        // }
        
    }
}