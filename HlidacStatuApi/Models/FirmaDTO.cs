namespace HlidacStatuApi.Models
{
    public class FirmaDTO
    {
        public string ICO { get; set; }
        public string Jmeno { get; set; }
        public string[] DatoveSchranky { get; set; }
        public DateTime? Zalozena { get; set; }
    }
}