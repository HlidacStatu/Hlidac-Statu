﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace InsolvencniRejstrik.Isir
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://isirws.cca.cz/", ConfigurationName="InsolvencniRejstrik.Isir.IsirWsCuzkPortType")]
    public interface IsirWsCuzkPortType
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        InsolvencniRejstrik.Isir.getIsirWsCuzkDataResponse getIsirWsCuzkData(InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest request);
        
        // CODEGEN: Generating message contract since the operation has multiple return values.
        [System.ServiceModel.OperationContractAttribute(Action="", ReplyAction="*")]
        System.Threading.Tasks.Task<InsolvencniRejstrik.Isir.getIsirWsCuzkDataResponse> getIsirWsCuzkDataAsync(InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://isirws.cca.cz/types/")]
    public enum priznakType
    {
        
        /// <remarks/>
        T,
        
        /// <remarks/>
        F,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://isirws.cca.cz/types/")]
    public partial class isirWsCuzkData
    {
        
        private string icField;
        
        private string rcField;
        
        private int cisloSenatuField;
        
        private string druhVecField;
        
        private int bcVecField;
        
        private int rocnikField;
        
        private string nazevOrganizaceField;
        
        private System.DateTime datumNarozeniField;
        
        private bool datumNarozeniFieldSpecified;
        
        private string titulPredField;
        
        private string titulZaField;
        
        private string jmenoField;
        
        private string nazevOsobyField;
        
        private string druhAdresyField;
        
        private string mestoField;
        
        private string uliceField;
        
        private string cisloPopisneField;
        
        private string okresField;
        
        private string zemeField;
        
        private string pscField;
        
        private string druhStavKonkursuField;
        
        private string urlDetailRizeniField;
        
        private priznakType dalsiDluznikVRizeniField;
        
        private bool dalsiDluznikVRizeniFieldSpecified;
        
        private System.DateTime datumPmZahajeniUpadkuField;
        
        private bool datumPmZahajeniUpadkuFieldSpecified;
        
        private System.DateTime datumPmUkonceniUpadkuField;
        
        private bool datumPmUkonceniUpadkuFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string ic
        {
            get
            {
                return this.icField;
            }
            set
            {
                this.icField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string rc
        {
            get
            {
                return this.rcField;
            }
            set
            {
                this.rcField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public int cisloSenatu
        {
            get
            {
                return this.cisloSenatuField;
            }
            set
            {
                this.cisloSenatuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public string druhVec
        {
            get
            {
                return this.druhVecField;
            }
            set
            {
                this.druhVecField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public int bcVec
        {
            get
            {
                return this.bcVecField;
            }
            set
            {
                this.bcVecField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public int rocnik
        {
            get
            {
                return this.rocnikField;
            }
            set
            {
                this.rocnikField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=6)]
        public string nazevOrganizace
        {
            get
            {
                return this.nazevOrganizaceField;
            }
            set
            {
                this.nazevOrganizaceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="date", Order=7)]
        public System.DateTime datumNarozeni
        {
            get
            {
                return this.datumNarozeniField;
            }
            set
            {
                this.datumNarozeniField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool datumNarozeniSpecified
        {
            get
            {
                return this.datumNarozeniFieldSpecified;
            }
            set
            {
                this.datumNarozeniFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=8)]
        public string titulPred
        {
            get
            {
                return this.titulPredField;
            }
            set
            {
                this.titulPredField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=9)]
        public string titulZa
        {
            get
            {
                return this.titulZaField;
            }
            set
            {
                this.titulZaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=10)]
        public string jmeno
        {
            get
            {
                return this.jmenoField;
            }
            set
            {
                this.jmenoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=11)]
        public string nazevOsoby
        {
            get
            {
                return this.nazevOsobyField;
            }
            set
            {
                this.nazevOsobyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=12)]
        public string druhAdresy
        {
            get
            {
                return this.druhAdresyField;
            }
            set
            {
                this.druhAdresyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=13)]
        public string mesto
        {
            get
            {
                return this.mestoField;
            }
            set
            {
                this.mestoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=14)]
        public string ulice
        {
            get
            {
                return this.uliceField;
            }
            set
            {
                this.uliceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=15)]
        public string cisloPopisne
        {
            get
            {
                return this.cisloPopisneField;
            }
            set
            {
                this.cisloPopisneField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=16)]
        public string okres
        {
            get
            {
                return this.okresField;
            }
            set
            {
                this.okresField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=17)]
        public string zeme
        {
            get
            {
                return this.zemeField;
            }
            set
            {
                this.zemeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=18)]
        public string psc
        {
            get
            {
                return this.pscField;
            }
            set
            {
                this.pscField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=19)]
        public string druhStavKonkursu
        {
            get
            {
                return this.druhStavKonkursuField;
            }
            set
            {
                this.druhStavKonkursuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=20)]
        public string urlDetailRizeni
        {
            get
            {
                return this.urlDetailRizeniField;
            }
            set
            {
                this.urlDetailRizeniField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=21)]
        public priznakType dalsiDluznikVRizeni
        {
            get
            {
                return this.dalsiDluznikVRizeniField;
            }
            set
            {
                this.dalsiDluznikVRizeniField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool dalsiDluznikVRizeniSpecified
        {
            get
            {
                return this.dalsiDluznikVRizeniFieldSpecified;
            }
            set
            {
                this.dalsiDluznikVRizeniFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="date", Order=22)]
        public System.DateTime datumPmZahajeniUpadku
        {
            get
            {
                return this.datumPmZahajeniUpadkuField;
            }
            set
            {
                this.datumPmZahajeniUpadkuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool datumPmZahajeniUpadkuSpecified
        {
            get
            {
                return this.datumPmZahajeniUpadkuFieldSpecified;
            }
            set
            {
                this.datumPmZahajeniUpadkuFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="date", Order=23)]
        public System.DateTime datumPmUkonceniUpadku
        {
            get
            {
                return this.datumPmUkonceniUpadkuField;
            }
            set
            {
                this.datumPmUkonceniUpadkuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool datumPmUkonceniUpadkuSpecified
        {
            get
            {
                return this.datumPmUkonceniUpadkuFieldSpecified;
            }
            set
            {
                this.datumPmUkonceniUpadkuFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://isirws.cca.cz/types/")]
    public partial class isirWsCuzkStatus
    {
        
        private int pocetVysledkuField;
        
        private bool pocetVysledkuFieldSpecified;
        
        private int relevanceVysledkuField;
        
        private bool relevanceVysledkuFieldSpecified;
        
        private System.DateTime casSynchronizaceField;
        
        private bool casSynchronizaceFieldSpecified;
        
        private kodChybyType kodChybyField;
        
        private bool kodChybyFieldSpecified;
        
        private string textChybyField;
        
        private string popisChybyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public int pocetVysledku
        {
            get
            {
                return this.pocetVysledkuField;
            }
            set
            {
                this.pocetVysledkuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool pocetVysledkuSpecified
        {
            get
            {
                return this.pocetVysledkuFieldSpecified;
            }
            set
            {
                this.pocetVysledkuFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public int relevanceVysledku
        {
            get
            {
                return this.relevanceVysledkuField;
            }
            set
            {
                this.relevanceVysledkuField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool relevanceVysledkuSpecified
        {
            get
            {
                return this.relevanceVysledkuFieldSpecified;
            }
            set
            {
                this.relevanceVysledkuFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public System.DateTime casSynchronizace
        {
            get
            {
                return this.casSynchronizaceField;
            }
            set
            {
                this.casSynchronizaceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool casSynchronizaceSpecified
        {
            get
            {
                return this.casSynchronizaceFieldSpecified;
            }
            set
            {
                this.casSynchronizaceFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public kodChybyType kodChyby
        {
            get
            {
                return this.kodChybyField;
            }
            set
            {
                this.kodChybyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool kodChybySpecified
        {
            get
            {
                return this.kodChybyFieldSpecified;
            }
            set
            {
                this.kodChybyFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public string textChyby
        {
            get
            {
                return this.textChybyField;
            }
            set
            {
                this.textChybyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public string popisChyby
        {
            get
            {
                return this.popisChybyField;
            }
            set
            {
                this.popisChybyField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://isirws.cca.cz/types/")]
    public enum kodChybyType
    {
        
        /// <remarks/>
        WS1,
        
        /// <remarks/>
        WS2,
        
        /// <remarks/>
        WS3,
        
        /// <remarks/>
        WS4,
        
        /// <remarks/>
        SQL1,
        
        /// <remarks/>
        SERVER1,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="getIsirWsCuzkDataRequest", WrapperNamespace="http://isirws.cca.cz/types/", IsWrapped=true)]
    public partial class getIsirWsCuzkDataRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string ic;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=1)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string rc;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=2)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string druhVec;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=3)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int bcVec;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=4)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int rocnik;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=5)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string nazevOsoby;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=6)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string jmeno;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=7)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="date")]
        public System.DateTime datumNarozeni;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=8)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int maxPocetVysledku;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=9)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public InsolvencniRejstrik.Isir.priznakType filtrAktualniRizeni;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=10)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public InsolvencniRejstrik.Isir.priznakType vyhledatPresnouShoduJmen;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=11)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public InsolvencniRejstrik.Isir.priznakType vyhledatBezDiakritiky;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=12)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int maxRelevanceVysledku;
        
        public getIsirWsCuzkDataRequest()
        {
        }
        
        public getIsirWsCuzkDataRequest(string ic, string rc, string druhVec, int bcVec, int rocnik, string nazevOsoby, string jmeno, System.DateTime datumNarozeni, int maxPocetVysledku, InsolvencniRejstrik.Isir.priznakType filtrAktualniRizeni, InsolvencniRejstrik.Isir.priznakType vyhledatPresnouShoduJmen, InsolvencniRejstrik.Isir.priznakType vyhledatBezDiakritiky, int maxRelevanceVysledku)
        {
            this.ic = ic;
            this.rc = rc;
            this.druhVec = druhVec;
            this.bcVec = bcVec;
            this.rocnik = rocnik;
            this.nazevOsoby = nazevOsoby;
            this.jmeno = jmeno;
            this.datumNarozeni = datumNarozeni;
            this.maxPocetVysledku = maxPocetVysledku;
            this.filtrAktualniRizeni = filtrAktualniRizeni;
            this.vyhledatPresnouShoduJmen = vyhledatPresnouShoduJmen;
            this.vyhledatBezDiakritiky = vyhledatBezDiakritiky;
            this.maxRelevanceVysledku = maxRelevanceVysledku;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="getIsirWsCuzkDataResponse", WrapperNamespace="http://isirws.cca.cz/types/", IsWrapped=true)]
    public partial class getIsirWsCuzkDataResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("data", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=true)]
        public InsolvencniRejstrik.Isir.isirWsCuzkData[] data;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://isirws.cca.cz/types/", Order=1)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public InsolvencniRejstrik.Isir.isirWsCuzkStatus stav;
        
        public getIsirWsCuzkDataResponse()
        {
        }
        
        public getIsirWsCuzkDataResponse(InsolvencniRejstrik.Isir.isirWsCuzkData[] data, InsolvencniRejstrik.Isir.isirWsCuzkStatus stav)
        {
            this.data = data;
            this.stav = stav;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    public interface IsirWsCuzkPortTypeChannel : InsolvencniRejstrik.Isir.IsirWsCuzkPortType, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.3")]
    public partial class IsirWsCuzkPortTypeClient : System.ServiceModel.ClientBase<InsolvencniRejstrik.Isir.IsirWsCuzkPortType>, InsolvencniRejstrik.Isir.IsirWsCuzkPortType
    {
        
        /// <summary>
        /// Implement this partial method to configure the service endpoint.
        /// </summary>
        /// <param name="serviceEndpoint">The endpoint to configure</param>
        /// <param name="clientCredentials">The client credentials</param>
        static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials);
        
        public IsirWsCuzkPortTypeClient() : 
                base(IsirWsCuzkPortTypeClient.GetDefaultBinding(), IsirWsCuzkPortTypeClient.GetDefaultEndpointAddress())
        {
            this.Endpoint.Name = EndpointConfiguration.IsirWsCuzkPortType.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public IsirWsCuzkPortTypeClient(EndpointConfiguration endpointConfiguration) : 
                base(IsirWsCuzkPortTypeClient.GetBindingForEndpoint(endpointConfiguration), IsirWsCuzkPortTypeClient.GetEndpointAddress(endpointConfiguration))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public IsirWsCuzkPortTypeClient(EndpointConfiguration endpointConfiguration, string remoteAddress) : 
                base(IsirWsCuzkPortTypeClient.GetBindingForEndpoint(endpointConfiguration), new System.ServiceModel.EndpointAddress(remoteAddress))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public IsirWsCuzkPortTypeClient(EndpointConfiguration endpointConfiguration, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(IsirWsCuzkPortTypeClient.GetBindingForEndpoint(endpointConfiguration), remoteAddress)
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public IsirWsCuzkPortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        InsolvencniRejstrik.Isir.getIsirWsCuzkDataResponse InsolvencniRejstrik.Isir.IsirWsCuzkPortType.getIsirWsCuzkData(InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest request)
        {
            return base.Channel.getIsirWsCuzkData(request);
        }
        
        public InsolvencniRejstrik.Isir.isirWsCuzkData[] getIsirWsCuzkData(string ic, string rc, string druhVec, int bcVec, int rocnik, string nazevOsoby, string jmeno, System.DateTime datumNarozeni, int maxPocetVysledku, InsolvencniRejstrik.Isir.priznakType filtrAktualniRizeni, InsolvencniRejstrik.Isir.priznakType vyhledatPresnouShoduJmen, InsolvencniRejstrik.Isir.priznakType vyhledatBezDiakritiky, int maxRelevanceVysledku, out InsolvencniRejstrik.Isir.isirWsCuzkStatus stav)
        {
            InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest inValue = new InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest();
            inValue.ic = ic;
            inValue.rc = rc;
            inValue.druhVec = druhVec;
            inValue.bcVec = bcVec;
            inValue.rocnik = rocnik;
            inValue.nazevOsoby = nazevOsoby;
            inValue.jmeno = jmeno;
            inValue.datumNarozeni = datumNarozeni;
            inValue.maxPocetVysledku = maxPocetVysledku;
            inValue.filtrAktualniRizeni = filtrAktualniRizeni;
            inValue.vyhledatPresnouShoduJmen = vyhledatPresnouShoduJmen;
            inValue.vyhledatBezDiakritiky = vyhledatBezDiakritiky;
            inValue.maxRelevanceVysledku = maxRelevanceVysledku;
            InsolvencniRejstrik.Isir.getIsirWsCuzkDataResponse retVal = ((InsolvencniRejstrik.Isir.IsirWsCuzkPortType)(this)).getIsirWsCuzkData(inValue);
            stav = retVal.stav;
            return retVal.data;
        }
        
        public System.Threading.Tasks.Task<InsolvencniRejstrik.Isir.getIsirWsCuzkDataResponse> getIsirWsCuzkDataAsync(InsolvencniRejstrik.Isir.getIsirWsCuzkDataRequest request)
        {
            return base.Channel.getIsirWsCuzkDataAsync(request);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        public virtual System.Threading.Tasks.Task CloseAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
        
        private static System.ServiceModel.Channels.Binding GetBindingForEndpoint(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.IsirWsCuzkPortType))
            {
                System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
                result.MaxBufferSize = int.MaxValue;
                result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
                result.MaxReceivedMessageSize = int.MaxValue;
                result.AllowCookies = true;
                result.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
                return result;
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.EndpointAddress GetEndpointAddress(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.IsirWsCuzkPortType))
            {
                return new System.ServiceModel.EndpointAddress("https://isir.justice.cz:8443/isir_cuzk_ws/IsirWsCuzkService");
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.Channels.Binding GetDefaultBinding()
        {
            return IsirWsCuzkPortTypeClient.GetBindingForEndpoint(EndpointConfiguration.IsirWsCuzkPortType);
        }
        
        private static System.ServiceModel.EndpointAddress GetDefaultEndpointAddress()
        {
            return IsirWsCuzkPortTypeClient.GetEndpointAddress(EndpointConfiguration.IsirWsCuzkPortType);
        }
        
        public enum EndpointConfiguration
        {
            
            IsirWsCuzkPortType,
        }
    }
}