namespace HlidacStatu.Entities.Dotace
{
    public class Prijemce
    {
        [Nest.Keyword]
        public string Ico { get; set; }
        [Nest.Text]
        public string ObchodniJmeno { get; set; }
        [Nest.Text]
        public string HlidacJmeno { get; set; }
        [Nest.Text]
        public string Jmeno { get; set; }
        [Nest.Number]
        public int? RokNarozeni { get; set; }

        [Nest.Keyword]
        public string Obec { get; set; }
        [Nest.Keyword]
        public string Okres { get; set; }
        [Nest.Keyword]
        public string PSC { get; set; }
        [Nest.Text]
        public string Ulice { get; set; }
    }
}
