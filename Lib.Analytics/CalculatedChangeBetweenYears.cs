using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Analytics
{
    public class CalculatedChangeBetweenYears
    {
        public decimal valueChange { get; set; }
        public decimal? percChange { get; set; }
        public int firstYear { get; set; }
        public int lastYear { get; set; }

    }


    public class CalculatedChangeBetweenYears<T>
        : CalculatedChangeBetweenYears
    {
        public T data { get; set; }
    }
}