namespace HlidacStatu.RegistrVozidel
{

    public partial class Importer
    {
        public class OpenDataDownload
        {



            public List<OpenDataFile> MesicniDavka { get; set; } = new();
            public DateTime MesicRok { get; set; }
            public class OpenDataFile
            {
                public enum Typy : int
                {
                    vypis_vozidel = 3,
                    technicke_prohlidky = 4,
                    vozidla_vyrazena_z_provozu = 6,
                    vozidla_dovoz = 7,
                    vozidla_doplnkove_vybaveni = 8,
                    zpravy_vyrobce_zastupce = 9,
                    vlastnik_provozovatel_vozidla = 10
                }

                string _directory = "";
                public string Directory
                {
                    get
                    {
                        return _directory;
                    }
                    set
                    {
                        var tmp = value;
                        if (tmp.EndsWith(System.IO.Path.DirectorySeparatorChar) == false)
                            tmp += System.IO.Path.DirectorySeparatorChar;
                        _directory = tmp;
                    }

                }
                public string Nazev { get; set; }
                public string NormalizedNazev { get => Nazev.ToLowerInvariant(); }
                public string Guid { get; set; }
                public Typy Typ { get; set; }
                public DateTime Vygenerovano { get; set; }
                public int Skip { get; set; }
            }
        }
    }
}
