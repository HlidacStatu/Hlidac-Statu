using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities
{
    //migrace: tahle classa slouží pouze jako view - do nových entit bych dal views asi do vlastní složky (Entities.csproj/Views)
    [Keyless]
    public class FindPersonDTO
    {
        public string NameId { get; set; }
        public string Jmeno { get; set; }
        public string Prijmeni { get; set; }
        public string JmenoAscii { get; set; }
        public string PrijmeniAscii { get; set; }
        public int? RokNarozeni { get; set; }
        public int? RokUmrti { get; set; }
        public string Aktpolstr { get; set; }
        public int? Pocet { get; set; }
    }
}