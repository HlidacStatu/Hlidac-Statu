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


        }
        public class GeneraModel<T> : GeneraModel
        {
            public T Data { get; set; } = default(T);
        }


    }
}
