namespace HlidacStatu.Web.Models
{
    public class DotaceAnalBoxes
    {
        public class GeneraModel
        {
            public string Query { get; set; }
            public int Height { get; set; } = 300;
            public string Title { get; set; } = "Dotace po letech";
            public int Top { get; set; } = int.MaxValue;

            public int TblOrderColumnIdx { get; set; } = 0;
            public string TblOrderDirection { get; set; } = "asc";
            public int TblPageLength { get; set; } = 10;
            public bool TblPaging { get; set; } = true;


        }
        public class GeneraModel<T> : GeneraModel
        {
            public T Data { get; set; } = default(T);
        }


    }
}
