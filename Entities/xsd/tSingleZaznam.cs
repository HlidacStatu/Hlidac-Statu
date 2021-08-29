namespace HlidacStatu.Entities.XSD
{

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/", IsNullable = false)]
    public partial class zaznam
    {



        /// <remarks/>
        public dumpZaznam data { get; set; }

        /// <remarks/>
        public zaznamPotvrzeni potvrzeni { get; set; }

    }




    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
    public partial class zaznamPotvrzeni
    {

        private zaznamPotvrzeniHash hashField;

        private string elektronickaZnackaField;

        /// <remarks/>
        public zaznamPotvrzeniHash hash
        {
            get
            {
                return hashField;
            }
            set
            {
                hashField = value;
            }
        }

        /// <remarks/>
        public string elektronickaZnacka
        {
            get
            {
                return elektronickaZnackaField;
            }
            set
            {
                elektronickaZnackaField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
    public partial class zaznamPotvrzeniHash
    {

        private string algoritmusField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string algoritmus
        {
            get
            {
                return algoritmusField;
            }
            set
            {
                algoritmusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
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



}
