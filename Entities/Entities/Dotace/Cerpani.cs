namespace HlidacStatu.Entities.Dotace
{
    public class Cerpani
    {
        [Nest.Number]
        public decimal? CastkaSpotrebovana { get; set; }
        [Nest.Number]
        public int? Rok { get; set; }
        [Nest.Number]
        public int? GuessedYear { get; set; }
    }
}
