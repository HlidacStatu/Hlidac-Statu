using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HlidacStatu.DetectJobs;
using HlidacStatu.JobTableEditor.Annotations;

namespace HlidacStatu.JobTableEditor.Data
{
    public class CellShell : INotifyPropertyChanged
    {
        private string value;
        private InHtmlTables.Cell.GuessedCellType cellType;
        private string error;
        
        public InHtmlTables.Cell Cell { get; private set; }
        public string Id { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }

        public InHtmlTables.Cell.GuessedCellType CellType
        {
            get => cellType;
            set
            {
                if (value == cellType) return;
                cellType = value;
                OnPropertyChanged(nameof(CellType));
            }
        }

        public string Value
        {
            get => value;
            set
            {
                if (value == this.value) return;
                this.value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string Error
        {
            get => error;
            set
            {
                if (value == error) return;
                error = value;
                OnPropertyChanged(nameof(Error));
            }
        }

        public CellShell(InHtmlTables.Cell cell, int row, int column)
        {
            Row = row;
            Column = column;
            Cell = cell;
            Id = $"[{Row}][{Column}]";
            CellType = cell.CellType;
            SetMergedValue(); //pre-set Value to be displayed
        }

        public void RotateType()
        {
            var allEnums = Enum.GetValues<InHtmlTables.Cell.GuessedCellType>();
            int nextPosition = Array.IndexOf(allEnums, CellType) + 1;
            CellType = nextPosition == allEnums.Length ? allEnums[0] : allEnums[nextPosition];
        }

        public void SetMergedValue()
        {
            Value = CellType switch
            {
                InHtmlTables.Cell.GuessedCellType.Position => string.Join(' ', Cell.FoundKeywords.Keys),
                InHtmlTables.Cell.GuessedCellType.Price or
                    InHtmlTables.Cell.GuessedCellType.PriceWithVAT => Cell.FoundNumbers.FirstOrDefault().ToString("N2"),
                _ => Cell.Text
            };
        }

        public void SetOriginalValue()
        {
            Value = Cell.Text;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}