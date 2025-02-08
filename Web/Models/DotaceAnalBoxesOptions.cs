namespace HlidacStatu.Web.Models
{
    public class DotaceAnalBoxes
    {
        public class ChartPoLetech
        {
            public string Query { get; set; }
            public int Height { get; set; } = 300;
            public string Title { get; set; } = "Dotace po letech";

        }
        public class TopProgramy
        {
            public string Query { get; set; }
            public int Top { get; set; } = int.MaxValue;

        }

    }
}
