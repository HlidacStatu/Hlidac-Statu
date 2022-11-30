using System.IO;


namespace HlidacStatu.Connectors
{
    public class Init
    {
        public static IO.PrilohaFile PrilohaLocalCopy = new IO.PrilohaFile();
        public static IO.OsobaFotkyFile OsobaFotky = new IO.OsobaFotkyFile();
        public static IO.UploadedTmpFile UploadedTmp = new IO.UploadedTmpFile();

        public static string WebAppDataPath = null;
        public static string WebAppRoot = null;

        static object lockObj = new object();
        static bool initialised = false;
        static Init()
        {
            lock (lockObj)
            {
                //TelemetryConfiguration.Active.InstrumentationKey = " your key ";
                if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("NewtonsoftJsonSchemaLicense")))
                    Newtonsoft.Json.Schema.License.RegisterLicense(Devmasters.Config.GetWebConfigValue("NewtonsoftJsonSchemaLicense"));

                if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("WebAppDataPath")))
                {
                    WebAppDataPath = Devmasters.Config.GetWebConfigValue("WebAppDataPath");
                }
                else throw new System.IO.DirectoryNotFoundException("cannot find WebAppDataPath");

                if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("WebAppRoot")))
                {
                    WebAppRoot = Devmasters.Config.GetWebConfigValue("WebAppRoot");
                }
                else throw new System.IO.DirectoryNotFoundException("cannot find WebAppRoot");

                WebAppRoot = Path.Combine(new System.IO.DirectoryInfo(WebAppRoot).FullName, "wwwroot");

                
                if (!WebAppRoot.EndsWith(Path.DirectorySeparatorChar))
                    WebAppRoot = WebAppRoot + Path.DirectorySeparatorChar;

                if (!WebAppDataPath.EndsWith(Path.DirectorySeparatorChar))
                    WebAppDataPath = WebAppDataPath + Path.DirectorySeparatorChar;

            }
        }


        public static void Initialise(string webAppDataPath = null)
        {
            lock (lockObj)
            {
                if (initialised == false)
                {

                    initialised = true;
                }
            }

        }



    }
}
