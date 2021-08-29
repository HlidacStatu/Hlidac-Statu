using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.IO;

namespace HlidacStatu.Connectors
{
    public static class InitializeConfigServices
    {
        public static void Initialize(string[] appSettingsVariants = null)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

            // build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false);

            if (appSettingsVariants != null)
            {
                foreach (var variant in appSettingsVariants)
                {
                    configuration = configuration.AddJsonFile($"appsettings.{variant}.json", true);
                }
            }

            // create service collection
            var services = new ServiceCollection();
            services.AddOptions();

            //init config
            Devmasters.Config.Init(configuration.Build());

            // create service provider
            var serviceProvider = services.BuildServiceProvider();
        }
    }
}