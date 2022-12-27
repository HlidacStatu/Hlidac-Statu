using Devmasters.Log;

using System;
using System.Text.RegularExpressions;

namespace HlidacStatu.Util
{
    public static class Consts
    {

        public static char Ch = 'Ȼ';

        public static Devmasters.Batch.MultiOutputWriter outputWriter =
            new Devmasters.Batch.MultiOutputWriter(
                Devmasters.Batch.Manager.DefaultOutputWriter,
                new Devmasters.Batch.LoggerWriter(Logger).OutputWriter
            );

        public static Devmasters.Batch.MultiProgressWriter progressWriter =
            new Devmasters.Batch.MultiProgressWriter(
                new Devmasters.Batch.ActionProgressWriter(0.1f).Write,
                new Devmasters.Batch.ActionProgressWriter(10, new Devmasters.Batch.LoggerWriter(Logger).ProgressWriter).Write
            );

        public static RegexOptions DefaultRegexQueryOption = RegexOptions.IgnoreCase
                                                            | RegexOptions.IgnorePatternWhitespace
                                                            | RegexOptions.Multiline;

        public static System.Globalization.CultureInfo enCulture = System.Globalization.CultureInfo.InvariantCulture; //new System.Globalization.CultureInfo("en-US");
        public static System.Globalization.CultureInfo czCulture = System.Globalization.CultureInfo.GetCultureInfo("cs-CZ");
        public static System.Globalization.CultureInfo csCulture = System.Globalization.CultureInfo.GetCultureInfo("cs");
        public static Random Rnd = new Random();

        public static Devmasters.Log.Logger Logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .AddLogStash(new Uri("http://10.10.150.203:5000")) //todo: tohle předělat na centrální konfiguraci
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                                .AddFileLoggerFilePerLevel($"{Devmasters.Config.GetWebConfigValue("SerilogBasePath")}/HlidacStatu/Web", "slog.txt",
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: Serilog.RollingInterval.Day,
                                    fileSizeLimitBytes: null,
                                    retainedFileCountLimit: 9,
                                    shared: true
                                    ));

    }
}
