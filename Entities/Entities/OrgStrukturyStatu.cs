using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace HlidacStatu.Entities.OrgStrukturyStatu
{

    public partial class JednotkaOrganizacni
    {


        public int CelkemZamestnava()
        {
            int soucetPodrizenych = ZamestnavajiPodrizeneOrganizace();
            return soucetPodrizenych + mistoPracovniPocet + mistoSluzebniPocet;
        }

        public int ZamestnavajiPodrizeneOrganizace()
        {
            return PodrizeneOrganizace?.Sum(p => p.CelkemZamestnava()) ?? 0;
        }


        public int RidiSluzebnich()
        {
            return PodrizeneOrganizace?.Sum(p => p.mistoSluzebniPocet + p.RidiSluzebnich()) ?? 0;
        }

        public int RidiPracovnich()
        {
            return PodrizeneOrganizace?.Sum(p => p.mistoPracovniPocet + p.RidiPracovnich()) ?? 0;
        }

        public D3GraphHierarchy GenerateD3DataHierarchy()
        {
            List<D3GraphHierarchy> children = new List<D3GraphHierarchy>();
            if (!(PodrizeneOrganizace is null) && PodrizeneOrganizace.Length != 0)
            {
                foreach (var po in PodrizeneOrganizace)
                {
                    children.Add(po.GenerateD3DataHierarchy());
                }

            }

            string employs = "";
            if (mistoPracovniPocet > 0 || mistoSluzebniPocet > 0)
            {
                employs = $"pracovní: {mistoPracovniPocet}; služební: {mistoSluzebniPocet}";
            }

            string manages = "";
            int ridiPracovnich = RidiPracovnich();
            int ridiSluzebnich = RidiSluzebnich();

            if (ridiPracovnich > 0 || ridiSluzebnich > 0)
            {
                manages = $"podřízených p: {ridiPracovnich}; s: {ridiSluzebnich}";
            }

            var current = new D3GraphHierarchy()
            {
                name = oznaceni,
                size = CelkemZamestnava(),
                children = children,
                employs = employs,
                manages = manages

            };

            return current;
        }
        public Summary GetSummary()
        {
            var sum = new Summary();
            sum.Oddeleni = oznaceni.ToLower().Contains("oddělení") ? 1 : 0;
            sum.Odbory = oznaceni.ToLower().Contains("odbor") ? 1 : 0;
            sum.Sekce = oznaceni.ToLower().Contains("sekce") ? 1 : 0;
            sum.Jine = sum.Oddeleni + sum.Odbory + sum.Sekce == 0 ? 1 : 0;
            sum.PracovniPozice = mistoPracovniPocet;
            sum.SluzebniMista = mistoSluzebniPocet;


            if (!(PodrizeneOrganizace is null) && PodrizeneOrganizace.Length != 0)
            {
                foreach (var po in PodrizeneOrganizace)
                {
                    sum.Add(po.GetSummary());
                }
            }

            return sum;
        }

    }

    public class Summary
    {
        internal Summary() { }
        public Summary(IEnumerable<JednotkaOrganizacni> urady)
        : this()
        {
            if (urady == null)
                throw new System.ArgumentNullException();
            Urady = urady.Count();

            foreach (var os in urady)
            {
                Add(os.GetSummary());
            }

        }
        public Summary Add(Summary sum)
        {
            Jine += sum.Jine;
            Odbory += sum.Odbory;
            Oddeleni += sum.Oddeleni;
            Urady += sum.Urady;
            PracovniPozice += sum.PracovniPozice;
            Sekce += sum.Sekce;
            SluzebniMista += sum.SluzebniMista;
            return this;
        }
        public int Jine { get; set; }
        public int Sekce { get; set; }
        public int Urady { get; set; }
        public int Odbory { get; set; }
        public int Oddeleni { get; set; }
        public int OrganizacniJednotky { get { return Jine + Sekce + Odbory + Oddeleni; } }
        public int PracovniPozice { get; set; }
        public int SluzebniMista { get; set; }

        public string Description(string ico)
        {
            var ret = $"Organizace je tvořena "
                + (Urady == 0 ? "" : $" {Devmasters.Lang.CS.Plural.Get(Urady, "jedním úřadem", "{0} úřady", "{0} úřady")}, dále ")
                + $"{Devmasters.Lang.CS.Plural.Get(OrganizacniJednotky, "jednou organizační části", "{0} organizačními částmi", "{0} organizačními částmi")}, ";
            return ret;


        }
        public string HtmlDescription(string ico)
        {
            var ret = $"Organizace je tvořena<ul>"
                + (Urady == 0 ? "" : $"<li><a href='/subjekt/DalsiInformace/{ico}'>{Devmasters.Lang.CS.Plural.Get(Urady, "jedním úřadem", "{0} úřady", "{0} úřady")}</a></li>")
                + $"<li><a href='/subjekt/OrganizacniStruktura/{ico}'>{Devmasters.Lang.CS.Plural.Get(OrganizacniJednotky, "<b>jednou</b> organizační části", "<b>{0}</b> organizačními částmi", "<b>{0}</b> organizačními částmi")}</a></li>"
                + "</ul>";
            return ret;


        }
    }

    public class D3GraphHierarchy
    {
        public string name { get; set; }
        public string employs { get; set; }
        public string manages { get; set; }
        public List<D3GraphHierarchy> children { get; set; }
        public int size { get; set; }
    }

    public partial class organizacni_struktura_sluzebnich_uradu
    {
    }

    public partial class ExportInfo
    {

    }

    /// <remarks/>
    public partial class UradSluzebniSeznam
    {
    }

    public partial class UradSluzebni
    {
    }

    public partial class UradSluzebniStrukturaOrganizacni
    {
    }

    public partial class StrukturaOrganizacni
    {
    }

    /// <remarks/>
    public partial class StrukturaOrganizacniPlocha
    {
    }

}