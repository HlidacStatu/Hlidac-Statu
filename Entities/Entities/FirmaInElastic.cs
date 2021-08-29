using Nest;

namespace HlidacStatu.Entities
{
    [ElasticsearchType(IdProperty = nameof(Ico))]
    public class FirmaInElastic
    {
        public string Jmeno { get; set; }

        public string JmenoBezKoncovky { get; set; }

        [Keyword] public string JmenoBezKoncovkyAscii { get; set; }

        [Keyword()] public string Ico { get; set; }


        private FirmaInElastic()
        {
        }

        public FirmaInElastic(Firma f)
        {
            Ico = f.ICO;
            Jmeno = f.Jmeno;
            JmenoBezKoncovky = f.JmenoBezKoncovky();
            JmenoBezKoncovkyAscii = Devmasters.TextUtil.RemoveDiacritics(JmenoBezKoncovky);
        }

        public FirmaInElastic(string ico, string jmeno)
        {
            Ico = ico;
            Jmeno = jmeno;
            JmenoBezKoncovky = Firma.JmenoBezKoncovky(jmeno);
            JmenoBezKoncovkyAscii = Devmasters.TextUtil.RemoveDiacritics(JmenoBezKoncovky);
        }


    }
}