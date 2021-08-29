using System.IO;


namespace HlidacStatu.Connectors
{
    //migrace: tohle by mohlo jít do XLibu a vše co je na tom závislý taky, protože to jsou až věci pro web
    public class Init
    {
        //public static Devmasters.Logging.Logger Logger = new Devmasters.Logging.Logger("HlidacSmluv");
        //public static System.Globalization.CultureInfo enCulture = System.Globalization.CultureInfo.InvariantCulture; //new System.Globalization.CultureInfo("en-US");
        //public static System.Globalization.CultureInfo czCulture = new System.Globalization.CultureInfo("cs-CZ");
        //public static Random Rnd = new Random();

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

                if (!WebAppRoot.EndsWith("\\"))
                    WebAppRoot = WebAppRoot + "\\";

                if (!WebAppDataPath.EndsWith("\\"))
                    WebAppDataPath = WebAppDataPath + "\\";

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
