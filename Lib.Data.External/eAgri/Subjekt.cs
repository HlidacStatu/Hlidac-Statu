namespace HlidacStatu.Lib.Data.External.eAgri.DeMinimis
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.pds.eu/RDM_PUB01B", IsNullable = false)]
    public partial class Response
    {

        private byte pocetField;

        private ResponseSeznam_subjektu seznam_subjektuField;

        /// <remarks/>
        public byte pocet
        {
            get
            {
                return pocetField;
            }
            set
            {
                pocetField = value;
            }
        }

        /// <remarks/>
        public ResponseSeznam_subjektu seznam_subjektu
        {
            get
            {
                return seznam_subjektuField;
            }
            set
            {
                seznam_subjektuField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektu
    {

        private ResponseSeznam_subjektuSubjekt subjektField;

        /// <remarks/>
        public ResponseSeznam_subjektuSubjekt subjekt
        {
            get
            {
                return subjektField;
            }
            set
            {
                subjektField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjekt
    {

        private uint subjektidField;

        private string obchodni_jmenoField;

        private ResponseSeznam_subjektuSubjektSeznam_identifikatoru seznam_identifikatoruField;

        private ResponseSeznam_subjektuSubjektAdresa adresaField;

        private ResponseSeznam_subjektuSubjektOblast_stav[] limity_stavField;

        private ResponseSeznam_subjektuSubjektNarizeni_stav[] limity_stav_narizeniField;

        private ResponseSeznam_subjektuSubjektPodpora[] seznam_podporField;

        private System.DateTime datum_zahajeni_cinnostiField;

        private ResponseSeznam_subjektuSubjektUcetni_obdobi[] seznam_uoField;

        /// <remarks/>
        public uint subjektid
        {
            get
            {
                return subjektidField;
            }
            set
            {
                subjektidField = value;
            }
        }

        /// <remarks/>
        public string obchodni_jmeno
        {
            get
            {
                return obchodni_jmenoField;
            }
            set
            {
                obchodni_jmenoField = value;
            }
        }

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektSeznam_identifikatoru seznam_identifikatoru
        {
            get
            {
                return seznam_identifikatoruField;
            }
            set
            {
                seznam_identifikatoruField = value;
            }
        }

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektAdresa adresa
        {
            get
            {
                return adresaField;
            }
            set
            {
                adresaField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("oblast_stav", IsNullable = false)]
        public ResponseSeznam_subjektuSubjektOblast_stav[] limity_stav
        {
            get
            {
                return limity_stavField;
            }
            set
            {
                limity_stavField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("narizeni_stav", IsNullable = false)]
        public ResponseSeznam_subjektuSubjektNarizeni_stav[] limity_stav_narizeni
        {
            get
            {
                return limity_stav_narizeniField;
            }
            set
            {
                limity_stav_narizeniField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("podpora", IsNullable = false)]
        public ResponseSeznam_subjektuSubjektPodpora[] seznam_podpor
        {
            get
            {
                return seznam_podporField;
            }
            set
            {
                seznam_podporField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime datum_zahajeni_cinnosti
        {
            get
            {
                return datum_zahajeni_cinnostiField;
            }
            set
            {
                datum_zahajeni_cinnostiField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ucetni_obdobi", IsNullable = false)]
        public ResponseSeznam_subjektuSubjektUcetni_obdobi[] seznam_uo
        {
            get
            {
                return seznam_uoField;
            }
            set
            {
                seznam_uoField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektSeznam_identifikatoru
    {

        private ResponseSeznam_subjektuSubjektSeznam_identifikatoruIdentifikator identifikatorField;

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektSeznam_identifikatoruIdentifikator identifikator
        {
            get
            {
                return identifikatorField;
            }
            set
            {
                identifikatorField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektSeznam_identifikatoruIdentifikator
    {

        private System.DateTime platnost_odField;

        private string typField;

        private uint valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime platnost_od
        {
            get
            {
                return platnost_odField;
            }
            set
            {
                platnost_odField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string typ
        {
            get
            {
                return typField;
            }
            set
            {
                typField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public uint Value
        {
            get
            {
                return valueField;
            }
            set
            {
                valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektAdresa
    {

        private string ulicenazField;

        private ushort cisdomhodField;

        private byte cisorhodField;

        private string cobcenazField;

        private string mcastnazField;

        private string obecnazField;

        private ushort pscField;

        private uint kodField;

        /// <remarks/>
        public string ulicenaz
        {
            get
            {
                return ulicenazField;
            }
            set
            {
                ulicenazField = value;
            }
        }

        /// <remarks/>
        public ushort cisdomhod
        {
            get
            {
                return cisdomhodField;
            }
            set
            {
                cisdomhodField = value;
            }
        }

        /// <remarks/>
        public byte cisorhod
        {
            get
            {
                return cisorhodField;
            }
            set
            {
                cisorhodField = value;
            }
        }

        /// <remarks/>
        public string cobcenaz
        {
            get
            {
                return cobcenazField;
            }
            set
            {
                cobcenazField = value;
            }
        }

        /// <remarks/>
        public string mcastnaz
        {
            get
            {
                return mcastnazField;
            }
            set
            {
                mcastnazField = value;
            }
        }

        /// <remarks/>
        public string obecnaz
        {
            get
            {
                return obecnazField;
            }
            set
            {
                obecnazField = value;
            }
        }

        /// <remarks/>
        public ushort psc
        {
            get
            {
                return pscField;
            }
            set
            {
                pscField = value;
            }
        }

        /// <remarks/>
        public uint kod
        {
            get
            {
                return kodField;
            }
            set
            {
                kodField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektOblast_stav
    {

        private System.DateTime platnostField;

        private string oblast_kodField;

        private uint limitField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime platnost
        {
            get
            {
                return platnostField;
            }
            set
            {
                platnostField = value;
            }
        }

        /// <remarks/>
        public string oblast_kod
        {
            get
            {
                return oblast_kodField;
            }
            set
            {
                oblast_kodField = value;
            }
        }

        /// <remarks/>
        public uint limit
        {
            get
            {
                return limitField;
            }
            set
            {
                limitField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektNarizeni_stav
    {

        private System.DateTime platnostField;

        private string kodField;

        private uint limitField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime platnost
        {
            get
            {
                return platnostField;
            }
            set
            {
                platnostField = value;
            }
        }

        /// <remarks/>
        public string kod
        {
            get
            {
                return kodField;
            }
            set
            {
                kodField = value;
            }
        }

        /// <remarks/>
        public uint limit
        {
            get
            {
                return limitField;
            }
            set
            {
                limitField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektPodpora
    {

        private string oblastField;

        private System.DateTime datum_prideleniField;

        private string menaField;

        private uint castka_kcField;

        private decimal castka_euroField;

        private byte forma_podporyField;

        private string ucel_podporyField;

        private byte pravni_akt_poskytnutiField;

        private ResponseSeznam_subjektuSubjektPodporaRezim_podpory rezim_podporyField;

        private uint id_podporyField;

        private uint poskytovatel_idField;

        private string poskytovatel_ojmField;

        private uint poskytovatel_icField;

        private byte stavField;

        private ResponseSeznam_subjektuSubjektPodporaSeznam_priznaky seznam_priznakyField;

        private System.DateTime insdatField;

        private System.DateTime edidatField;

        /// <remarks/>
        public string oblast
        {
            get
            {
                return oblastField;
            }
            set
            {
                oblastField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime datum_prideleni
        {
            get
            {
                return datum_prideleniField;
            }
            set
            {
                datum_prideleniField = value;
            }
        }

        /// <remarks/>
        public string mena
        {
            get
            {
                return menaField;
            }
            set
            {
                menaField = value;
            }
        }

        /// <remarks/>
        public uint castka_kc
        {
            get
            {
                return castka_kcField;
            }
            set
            {
                castka_kcField = value;
            }
        }

        /// <remarks/>
        public decimal castka_euro
        {
            get
            {
                return castka_euroField;
            }
            set
            {
                castka_euroField = value;
            }
        }

        /// <remarks/>
        public byte forma_podpory
        {
            get
            {
                return forma_podporyField;
            }
            set
            {
                forma_podporyField = value;
            }
        }

        /// <remarks/>
        public string ucel_podpory
        {
            get
            {
                return ucel_podporyField;
            }
            set
            {
                ucel_podporyField = value;
            }
        }

        /// <remarks/>
        public byte pravni_akt_poskytnuti
        {
            get
            {
                return pravni_akt_poskytnutiField;
            }
            set
            {
                pravni_akt_poskytnutiField = value;
            }
        }

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektPodporaRezim_podpory rezim_podpory
        {
            get
            {
                return rezim_podporyField;
            }
            set
            {
                rezim_podporyField = value;
            }
        }

        /// <remarks/>
        public uint id_podpory
        {
            get
            {
                return id_podporyField;
            }
            set
            {
                id_podporyField = value;
            }
        }

        /// <remarks/>
        public uint poskytovatel_id
        {
            get
            {
                return poskytovatel_idField;
            }
            set
            {
                poskytovatel_idField = value;
            }
        }

        /// <remarks/>
        public string poskytovatel_ojm
        {
            get
            {
                return poskytovatel_ojmField;
            }
            set
            {
                poskytovatel_ojmField = value;
            }
        }

        /// <remarks/>
        public uint poskytovatel_ic
        {
            get
            {
                return poskytovatel_icField;
            }
            set
            {
                poskytovatel_icField = value;
            }
        }

        /// <remarks/>
        public byte stav
        {
            get
            {
                return stavField;
            }
            set
            {
                stavField = value;
            }
        }

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektPodporaSeznam_priznaky seznam_priznaky
        {
            get
            {
                return seznam_priznakyField;
            }
            set
            {
                seznam_priznakyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime insdat
        {
            get
            {
                return insdatField;
            }
            set
            {
                insdatField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime edidat
        {
            get
            {
                return edidatField;
            }
            set
            {
                edidatField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektPodporaRezim_podpory
    {

        private bool adhocField;

        /// <remarks/>
        public bool adhoc
        {
            get
            {
                return adhocField;
            }
            set
            {
                adhocField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektPodporaSeznam_priznaky
    {

        private ResponseSeznam_subjektuSubjektPodporaSeznam_priznakyPriznak priznakField;

        /// <remarks/>
        public ResponseSeznam_subjektuSubjektPodporaSeznam_priznakyPriznak priznak
        {
            get
            {
                return priznakField;
            }
            set
            {
                priznakField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektPodporaSeznam_priznakyPriznak
    {

        private byte kodField;

        private string nazevField;

        private string popisField;

        /// <remarks/>
        public byte kod
        {
            get
            {
                return kodField;
            }
            set
            {
                kodField = value;
            }
        }

        /// <remarks/>
        public string nazev
        {
            get
            {
                return nazevField;
            }
            set
            {
                nazevField = value;
            }
        }

        /// <remarks/>
        public string popis
        {
            get
            {
                return popisField;
            }
            set
            {
                popisField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.pds.eu/RDM_PUB01B")]
    public partial class ResponseSeznam_subjektuSubjektUcetni_obdobi
    {

        private System.DateTime datum_doField;

        private System.DateTime datum_odField;

        private uint id_uoField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime datum_do
        {
            get
            {
                return datum_doField;
            }
            set
            {
                datum_doField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime datum_od
        {
            get
            {
                return datum_odField;
            }
            set
            {
                datum_odField = value;
            }
        }

        /// <remarks/>
        public uint id_uo
        {
            get
            {
                return id_uoField;
            }
            set
            {
                id_uoField = value;
            }
        }
    }


}
