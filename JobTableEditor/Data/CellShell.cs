using System.ComponentModel;
using System.Runtime.CompilerServices;
using HlidacStatu.DetectJobs;

namespace JobTableEditor.Data
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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}