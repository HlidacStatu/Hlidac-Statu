using Devmasters.Log;

namespace HlidacStatuApi.Code
{
    public class Log
    {
        
        public static Devmasters.Log.Logger Logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.API.Web"
                    //,
                    //   Devmasters.Log.Logger.DefaultConfiguration()
                    //       .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                    //       .AddLogStash(new Uri("http://10.10.150.203:5000"))  //todo: tohle nastavit na centrální konfiguraci
                    //       .AddFileLoggerFilePerLevel($"{Devmasters.Config.GetWebConfigValue("SerilogBasePath")}/HlidacStatu/Api", "slog.txt",
                    //           outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    //           rollingInterval: Serilog.RollingInterval.Day,
                    //           fileSizeLimitBytes: null,
                    //           retainedFileCountLimit: 9,                                   
                    //           shared: true
                    //           )
                    );
    }
}
