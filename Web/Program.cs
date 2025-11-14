using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace HlidacStatu.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            StartupLogger.Write("Application started.");
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            
            var builder = WebApplication.CreateBuilder(args);
            StartupLogger.Write("Builder created.");
            
            builder.ConfigureHostForWeb(args);
            StartupLogger.Write("Configuration loaded and Logger initialized.");
            
            builder.WebHost.UseStaticWebAssets();
            
#if DEBUG
            //dont check ssl for local debugging with local api
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif
            
            //get IConfiguration
            var configuration = builder.Configuration;
            StartupLogger.Write("Configuration 2 initialized.");
            
            //inicializace statických proměnných
            Devmasters.Config.Init(configuration);
            StartupLogger.Write("Devmasters Init inited");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;
            
            
            DBUpgrades.DBUpgrader.UpgradeDatabases(Connectors.DirectDB.Instance.DefaultCnnStr);
            StartupLogger.Write("Db upgrader upgraded.");
            
            builder.Services.ConfigureServices(configuration);
            StartupLogger.Write("Services configured.");
            
            WebApplication app = builder.Build();
            StartupLogger.Write("App builded");
            
            app.ConfigurePipeline();
            StartupLogger.Write("Pipeline configured");
            
            app.Run();
        }
    }
}