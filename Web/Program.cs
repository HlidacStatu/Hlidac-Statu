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
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            var builder = WebApplication.CreateBuilder(args);
            builder.ConfigureHostForWeb(args);
            builder.WebHost.UseStaticWebAssets();
            
#if DEBUG
            //dont check ssl for local debugging with local api
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif
            
            //get IConfiguration
            var configuration = builder.Configuration;
            
            //inicializace statických proměnných
            Devmasters.Config.Init(configuration);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;
            DBUpgrades.DBUpgrader.UpgradeDatabases(Connectors.DirectDB.Instance.DefaultCnnStr);
            
            builder.Services.ConfigureServices(configuration);

            WebApplication app = builder.Build();
            app.ConfigurePipeline();
            
            app.Run();
        }
    }
}