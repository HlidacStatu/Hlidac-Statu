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
        private string _value;
        private InHtmlTables.Cell.GuessedCellType _cellType;
        
        public InHtmlTables.Cell Cell { get; private set; }
        public string Id { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }

        public InHtmlTables.Cell.GuessedCellType CellType
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

        public string Error { get; set; }
        
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
                InHtmlTables.Cell.GuessedCellType.Position => Cell.Text,
                InHtmlTables.Cell.GuessedCellType.Price or
                    InHtmlTables.Cell.GuessedCellType.PriceWithVAT => Cell.FoundNumbers.FirstOrDefault().ToString("N2"),
                _ => Cell.OriginalCellText
            };
        }

        public void SetOriginalValue()
        {
            Value = Cell.OriginalCellText;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RunErrorChecks();
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RunErrorChecks()
        {
            Error = "";
            CheckAllowedValue(1000, 20000);
            CheckJobIsNotEmpty();

        }

        private void CheckJobIsNotEmpty()
        {
            if (CellType == InHtmlTables.Cell.GuessedCellType.Position)
            {
                if (string.IsNullOrWhiteSpace(Value))
                {
                    Error += "Není vyplněn název pozice.";
                }
            }
        }

        private void CheckAllowedValue(decimal from, decimal to)
        {
            decimal vat = 1.21m;
            decimal? value = Devmasters.ParseText.ToDecimal(Value);

            
            if (CellType == InHtmlTables.Cell.GuessedCellType.Price)
            {
                if (!value.HasValue)
                {
                    Error += "Chybí vyplněná hodnota.";
                    return;
                }
                
                if (value < from)
                {
                    Error += $"Zkontroluj hodnotu, zdá se nám nízká [{value:N1}].";
                    return;
                }
                
                if (value > to)
                {
                    Error += $"Zkontroluj hodnotu, zdá se nám vysoká [{value:N1}].";
                    return;
                }
            }
            
            if (CellType == InHtmlTables.Cell.GuessedCellType.PriceWithVAT)
            {
                if (!value.HasValue)
                {
                    Error += "Chybí vyplněná hodnota.";
                    return;
                }
                
                if (value < (from * vat))
                {
                    Error += $"Zkontroluj hodnotu, zdá se nám nízká [{value:N1}].";
                    return;
                }
                
                if (value > (to * vat))
                {
                    Error += $"Zkontroluj hodnotu, zdá se nám vysoká [{value:N1}].";
                    return;
                }
            }
            
        }
        
    }
}