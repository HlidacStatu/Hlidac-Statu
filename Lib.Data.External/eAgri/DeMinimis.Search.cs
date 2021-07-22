using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace HlidacStatu.Lib.Data.External.eAgri
{
    public static class DeMinimisCalls
    {
        

        //dn=""cn=PDS_RDM_All,cn=PDS,cn=Users,o=mze,c=cz"" certificateSN=""#NEDEF"" addressAD=""default"" certificateOwner=""#NEDEF""
        static string icoSubReq = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v02=""http://www.pds.eu/vOKO/v0200"" xmlns:rdm=""http://www.pds.eu/RDM_PUB01B"" xmlns:ns=""http://www.mze.cz/ESBServer/1.0"">
<soapenv:Header/>
<soapenv:Body>
<v02:Request vOKOid=""RDM_PUB01B"">
<v02:RequestContent>
<rdm:Request>
<rdm:dotaz_detail>
    <rdm:dotaz_ic>
        <rdm:ic>#ICO#</rdm:ic>
    </rdm:dotaz_ic>
</rdm:dotaz_detail>
</rdm:Request>
</v02:RequestContent>
</v02:Request>
</soapenv:Body>
</soapenv:Envelope>
";
        public static void SearchSubj(string ico)
        {

            string url = "https://eagri.cz/ssl/nosso-app/EPO/WS/v2Online/vOKOsrv.ashx";
            string req = icoSubReq.Replace("#ICO#",ico);

            Soap net = new Soap();
            string resp = net.UploadString(url, req);

            XElement xdoc = XElement.Parse(resp);
            var res = xdoc.DescendantNodes()
                .Select(m=>m as XElement)
                .Where(m=>m!=null && m.HasAttributes)
                .Where(m => m.Attributes()
                        .Any(a => a.Name == "xmlns" && a.Value == "http://www.pds.eu/RDM_PUB01B") == true
                    )
                    .ToArray();
                    var serializer = new XmlSerializer(typeof(DeMinimis.Response),"http://www.pds.eu/RDM_PUB01B");
        var xxx=  (DeMinimis.Response)serializer.Deserialize(res.First().CreateReader());

        }


        class Soap : WebClient
        {
            public int Timeout { get; set; }
            public Soap() : this(60 * 1000) { } //1min

            public Soap(int timeout)
            {
                Timeout = timeout;
                Encoding = Encoding.UTF8;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {

                var request = base.GetWebRequest(address);
                request.ContentType = "text/xml";
                if (request != null)
                {
                    ((HttpWebRequest)request).KeepAlive = false;
                    ((HttpWebRequest)request).ReadWriteTimeout = Timeout;
                    request.Timeout = Timeout;
                }
                return request;
            }

        }

        /// <summary>
        /// Vytvoří autentitační token pro volání služby
        /// </summary>
        /// <param name="key">Přístupový klíč</param>
        /// <param name="request">Request služby včetně elementu <SOAP:Envelope></SOAP:Envelope></param>
        /// <returns></returns>
        private static string _sign(string key, string request)
        {
            //z request se vytvoří XmlDocument
            XmlDocument xmlRequest = new XmlDocument();
            xmlRequest.LoadXml(request);

            //spočítá se SHA1 hash přístupového klíče
            SHA1 sha1 = SHA1.Create();
            byte[] hashKey = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));

            //z requestu se vytáhne element body včetně krajních XML značek - outer xml
            XmlDocument xmlBody = new XmlDocument();
            xmlBody.LoadXml(xmlRequest.GetElementsByTagName("Body", xmlRequest.DocumentElement.NamespaceURI)[0].OuterXml);

            //provede se kanonizace body
            XmlDsigC14NTransform tr = new XmlDsigC14NTransform();
            tr.LoadInput(xmlBody);
            MemoryStream transOutput = (MemoryStream)tr.GetOutput();
            byte[] bodyBytes = transOutput.ToArray(); ;

            //ke kanonizovanému body se bytově připojí SHA1 hash hesla
            byte[] combined = new byte[bodyBytes.Length + hashKey.Length];
            Buffer.BlockCopy(bodyBytes, 0, combined, 0, bodyBytes.Length);
            Buffer.BlockCopy(hashKey, 0, combined, bodyBytes.Length, hashKey.Length);

            //výsledek se zahashuje SHA512
            SHA512 sha512 = SHA512.Create();
            byte[] hashResult = sha512.ComputeHash(combined);
            //převede se do base64
            string result = Convert.ToBase64String(hashResult);

            return result;
        }

    }
}
