using System.Net.Mime;
using System.Text;

namespace BlurredPageMinion
{
    internal class ExceptionHandling
    {
        public static HttpClient _httpClient { get; set; }

        public static void SendLogToServer(string msg, HttpClient apiClient)
        {
            if (apiClient == null)
                return;
            try
            {
                msg = msg + "\n===========\nAPI Key:" + Settings.ApiKey;
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(msg), Encoding.UTF8, MediaTypeNames.Application.Json);
                var res = apiClient.PostAsync("https://api.hlidacstatu.cz/api/v2/bp/Log", content)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                res.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                var msgErr = $"Unexpected error during SendLogToServer. Exc:" + e.ToString();
                Console.WriteLine(msgErr);
                Console.Error.WriteLine(msgErr);
            }

        }


        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (Settings.Debug)
            {
                var msg = $"Expected process exit ({System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}).";
                msg = msg + "\n\n"
                    + Devmasters.Diags.Render(
                        Devmasters.Diags.GetOSInfo()
                            .Concat(Devmasters.Diags.GetProcessInfo())
                            .Concat(Devmasters.Diags.GetGarbageCollectorInfo())
                            .Concat(Devmasters.Diags.GetDrivesInfo())
                    );

                Console.WriteLine(msg);
                Devmasters.Log.Logger.Root.Debug(msg);
                //SendLogToServer(msg, _httpClient);
            }
        }

        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            if (ex.ExceptionObject != null && ex.ExceptionObject.GetType() == typeof(Exception))
            {
                string msg = $"Top level UnhandledException ({System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()})."
                                    + ((Exception)ex.ExceptionObject).ToString();
                msg = msg + "\n\n" 
                    + Devmasters.Diags.Render(
                        Devmasters.Diags.GetOSInfo()
                            .Concat(Devmasters.Diags.GetProcessInfo())
                            .Concat(Devmasters.Diags.GetGarbageCollectorInfo())
                            .Concat(Devmasters.Diags.GetDrivesInfo())
                    );
                Console.WriteLine(msg);
                Console.Error.WriteLine(msg);
                Devmasters.Log.Logger.Root.Fatal(msg);
                SendLogToServer(msg, _httpClient);

            }
            else
            {
                string msg = $"Top level UnhandledException ({System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()})."
                    + ex.ExceptionObject?.GetType()?.ToString();
                Console.WriteLine(msg);
                Console.Error.WriteLine(msg);
                Devmasters.Log.Logger.Root.Warning(msg);

                SendLogToServer(msg, _httpClient);

            }
        }
    }
}
