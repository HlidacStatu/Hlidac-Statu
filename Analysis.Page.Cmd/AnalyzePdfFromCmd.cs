namespace HlidacStatu.Analysis.Page.Area
{
    public partial class AnalyzePdfFromCmd 
    {

        static AnalyzePdfFromCmd()
        {
        }

        //public static Net SharedModel = null;



        public static AnalyzedPdf AnalyzePDF(string fileNameOfPDF, bool debug, out string output)
        {

            System.Diagnostics.ProcessStartInfo pi = null;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    var fn = Path.Combine(AppContext.BaseDirectory, "win-x64/AnalyzePageAreaCmd.exe");
                    pi = new System.Diagnostics.ProcessStartInfo(fn, $"/fn={fileNameOfPDF} {(debug ? "/debug" : "")}");
                    //pi.WorkingDirectory = Path.Combine(AppContext.BaseDirectory,"win-x64");
                    break;
                case PlatformID.Unix:
                    var fnL = Path.Combine(AppContext.BaseDirectory, "linux-x64/AnalyzePageAreaCmd");
                    pi = new System.Diagnostics.ProcessStartInfo(fnL, $"/fn={fileNameOfPDF} {(debug ? "/debug" : "")}");
                    pi.WorkingDirectory = Path.GetDirectoryName(fnL);
                    break;
                case PlatformID.MacOSX:
                    break;
                case PlatformID.Other:
                case PlatformID.Xbox:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                default:
                    break;
            }
            Devmasters.ProcessExecutor startProc = new Devmasters.ProcessExecutor(pi, 60 * 60 * 6);//6 hours
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            startProc.StandardOutputDataReceived += (o, e) =>
            {
                sb.AppendLine(e.Data);
                //currentSession.ScriptOutput += e.Data;
                if (debug)
                    Console.WriteLine(e.Data);
            };
            startProc.ErrorDataReceived += (o, e) =>
            {
                sb.AppendLine("ERR: "+e.Data);
                //currentSession.ScriptOutput += e.Data;
                if (e.Data?.Contains("Fontconfig error") == false)
                    Console.WriteLine(e.Data);
            };
            startProc.Start();

            output = sb.ToString();
            if (System.IO.File.Exists(fileNameOfPDF + ".json"))
            {
                AnalyzedPdf? ret = System.Text.Json.JsonSerializer.Deserialize<AnalyzedPdf?>(System.IO.File.ReadAllText(fileNameOfPDF + ".json"));
                Devmasters.IO.IOTools.DeleteFile(fileNameOfPDF + ".json");
                return ret;
            }
            else
            {
                return null;
            }

        }


    }
}
