using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace HlidacStatu.Entities.OrgStrukturyStatu
{



    // using System.Xml.Serialization;
    // XmlSerializer serializer = new XmlSerializer(typeof(organizacni_struktura_sluzebnich_uradu));
    // using (StringReader reader = new StringReader(xml))
    // {
    //    var test = (organizacni_struktura_sluzebnich_uradu)serializer.Deserialize(reader);
    // }

    [XmlRoot(ElementName = "ExportInfo", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
    public partial class ExportInfo
    {

        [XmlElement(ElementName = "ExportId", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public string ExportId { get; set; }

        [XmlElement(ElementName = "ZdrojDat", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public string ZdrojDat { get; set; }

        [XmlElement(ElementName = "ExportTyp", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public string ExportTyp { get; set; }

        [XmlElement(ElementName = "ExportPokyn", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public string ExportPokyn { get; set; }

        [XmlElement(ElementName = "ExportDatumCas", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public DateTime ExportDatumCas { get; set; }
    }

    [XmlRoot(ElementName = "UradSluzebni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
    public partial class UradSluzebni
    {

        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "idDS", Namespace = "")]
        public string idDS { get; set; }

        [XmlAttribute(AttributeName = "oznaceni", Namespace = "")]
        public string oznaceni { get; set; }

        [XmlAttribute(AttributeName = "zkratka", Namespace = "")]
        public string zkratka { get; set; }

        [XmlAttribute(AttributeName = "idNadrizene", Namespace = "")]
        public string idNadrizene { get; set; }
    }

    [XmlRoot(ElementName = "UradSluzebniSeznam", Namespace = "http://opendata.gov.cz/schema/xsd/typy/")]
    public partial class UradSluzebniSeznam
    {

        [XmlElement(ElementName = "UradSluzebni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public List<UradSluzebni> SluzebniUrady { get; set; }
    }

    [XmlRoot(ElementName = "JednotkaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
    public partial class JednotkaOrganizacni
    {

        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "idExterni", Namespace = "")]
        public string idExterni { get; set; }

        [XmlAttribute(AttributeName = "mistoPracovniPocet", Namespace = "")]
        public int mistoPracovniPocet { get; set; }

        [XmlAttribute(AttributeName = "mistoSluzebniPocet", Namespace = "")]
        public int mistoSluzebniPocet { get; set; }

        [XmlAttribute(AttributeName = "nazev", Namespace = "")]
        public string nazev { get; set; }

        [XmlAttribute(AttributeName = "oznaceni", Namespace = "")]
        public string oznaceni { get; set; }

        [XmlAttribute(AttributeName = "predstaveny", Namespace = "")]
        public bool predstaveny { get; set; }

        [XmlAttribute(AttributeName = "zkratka", Namespace = "")]
        public string zkratka { get; set; }

        [XmlAttribute(AttributeName = "vedouci", Namespace = "")]
        public bool vedouci { get; set; }

        [XmlElement(ElementName = "JednotkaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public JednotkaOrganizacni[] PodrizeneOrganizace { get; set; }

        [XmlAttribute(AttributeName = "idNadrizene", Namespace = "")]
        public string idNadrizene { get; set; }

        [XmlAttribute(AttributeName = "idNadrizeneExterni", Namespace = "")]
        public string idNadrizeneExterni { get; set; }
    }

    [XmlRoot(ElementName = "StrukturaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
    public partial class StrukturaOrganizacni
    {

        [XmlElement(ElementName = "JednotkaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public JednotkaOrganizacni HlavniOrganizacniJednotka{ get; set; }
    }

    [XmlRoot(ElementName = "StrukturaOrganizacniPlocha", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
    public partial class StrukturaOrganizacniPlocha
    {

        [XmlElement(ElementName = "JednotkaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public List<JednotkaOrganizacni> JednotkaOrganizacni { get; set; }
    }

    [XmlRoot(ElementName = "UradSluzebniStrukturaOrganizacni", Namespace = "http://opendata.gov.cz/schema/xsd/typy/")]
    public partial class UradSluzebniStrukturaOrganizacni
    {

        [XmlElement(ElementName = "StrukturaOrganizacni", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public StrukturaOrganizacni StrukturaOrganizacni { get; set; }

        [XmlElement(ElementName = "StrukturaOrganizacniPlocha", Namespace = "urn:cz:mvcr:isoss:schemas:Opendata:OSYS:OSSU:v1")]
        public StrukturaOrganizacniPlocha StrukturaOrganizacniPlocha { get; set; }

        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string id { get; set; }
    }

    [XmlRoot(ElementName = "organizacni_struktura_sluzebnich_uradu", Namespace = "http://opendata.gov.cz/schema/xsd/typy/")]
    public partial class organizacni_struktura_sluzebnich_uradu
    {

        [XmlElement(ElementName = "ExportInfo", Namespace = "urn:cz:mvcr:isoss:schemas:Common:v1")]
        public ExportInfo ExportInfo { get; set; }

        [XmlElement(ElementName = "UradSluzebniSeznam", Namespace = "http://opendata.gov.cz/schema/xsd/typy/")]
        public UradSluzebniSeznam UradSluzebniSeznam { get; set; }

        [XmlElement(ElementName = "UradSluzebniStrukturaOrganizacni", Namespace = "http://opendata.gov.cz/schema/xsd/typy/")]
        public List<UradSluzebniStrukturaOrganizacni> OrganizacniStruktura { get; set; }

        [XmlAttribute(AttributeName = "govcz", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string govcz { get; set; }

        [XmlAttribute(AttributeName = "cmn", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string cmn { get; set; }

        [XmlAttribute(AttributeName = "n", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string n { get; set; }

        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string xsi { get; set; }

        [XmlText]
        public string text { get; set; }
    }





}