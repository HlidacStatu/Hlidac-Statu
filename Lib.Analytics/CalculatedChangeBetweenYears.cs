using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Analytics
{
    public class CalculatedChangeBetweenYears
    {
        public int firstYear { get; set; }
        public int lastYear { get; set; }

        public ChangeInValues change { get; set; }

    }


    public class CalculatedChangeBetweenYears<T>
        : CalculatedChangeBetweenYears
    {
        public T data { get; set; }
    }
}