using Nest;

using System;

namespace HlidacStatu.Entities.VZ
{
    public class ProfilZadavatele
    {
        public string Id
        {
            get
            {
                return DataSet + "-" + EvidencniCisloProfilu;
            }
        }

        [Keyword]
        public string EvidencniCisloFormulare { get; set; }
        [Keyword]
        public string EvidencniCisloProfilu { get; set; }
        public VerejnaZakazka.Subject Zadavatel { get; set; } = new VerejnaZakazka.Subject();
        [Keyword]
        public string Url { get; set; }
        [Date()]
        public DateTime? DatumUverejneni { get; set; }

        [Date()]
        public DateTime? LastUpdate { get; set; }

        [Date()]
        public DateTime? LastAccess { get; set; }
        public enum LastAccessResults
        {
            OK = 0,
            HttpError = 1,
            XmlError = 2,
            OtherError = 3
        }

        //0=OK
        public LastAccessResults? LastAccessResult { get; set; }


        [Keyword]
        public string DataSet { get; set; }



    }

}
