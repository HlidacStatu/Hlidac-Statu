using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CsvHelper;
using HlidacStatu.Entities;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using MemoryStream = System.IO.MemoryStream;

class Program
{
    private static AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));
    
    // todo: add Devmasters init to load config for repositories.
    
    public static async Task Main(string[] args)
    {
        string ovmUrl = "https://www.czechpoint.cz/spravadat/ovm/datafile.do?format=xml&service=seznamovm"; 
        
        using var httpClient = new HttpClient();
        using var responseMessage = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync(ovmUrl));

        if (!responseMessage.IsSuccessStatusCode)
        {
            // _logger.Error("Error during {methodName}. Server responded with [{statusCode}] status code. Reason phrase [{reasonPhrase}].",
            //     nameof(GetNewTaskAsync), responseMessage.StatusCode, responseMessage.ReasonPhrase);
            return;
        }
        
        var stream = await responseMessage.Content.ReadAsStreamAsync();
        using (var gZipStream = new GZipStream(stream, CompressionMode.Decompress))
        using (var decompressedStream = new MemoryStream())
        {
            await gZipStream.CopyToAsync(decompressedStream);
            decompressedStream.Position = 0;
            using var text = new XmlTextReader(decompressedStream);
            text.Namespaces = false;
            
            
            XmlSerializer serializer = new XmlSerializer(typeof(SeznamOvmIndex));
            var test = (SeznamOvmIndex)serializer.Deserialize(text);
            
            using (var db = new DbEntities())
            {
                foreach (var ovm in test.Subjekt)
                {
                    db.OrganVerejneMoci.Add(ovm);
                }
                
                await db.SaveChangesAsync();
            }
        }
        
        
            
        
        
        // parse xml
        // stáhnout seznam ovm - https://www.czechpoint.cz/spravadat/ovm/datafile.do?format=xml&service=seznamovm
        // rozparsovat do subjektů
        // uložit podřízené objekty 
        // - TypOvm, PravniFormaOvm, AdresaOvm
        // a nakonec OVM
        

        // parse csv
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string csvFile = @"d:\Downloads\557072-Pardubice_V.csv";
        var culture = System.Globalization.CultureInfo.GetCultureInfo("cs");
        var encoding = Encoding.GetEncoding(1250);
        string delimiter = ";";

        var csvConfiguration = new CsvHelper.Configuration.CsvConfiguration(culture)
        {
            HasHeaderRecord = true, 
            Delimiter = delimiter
        };

        using (var reader = new StreamReader(csvFile, encoding))
        using (var csv = new CsvReader(reader, csvConfiguration))
        {
            var records = csv.GetRecords<AdresniMisto>();
            Console.WriteLine("parsed");
        }
    }
}


//stáhnout seznamy adresních míst - https://services.cuzk.cz/sestavy/VO/