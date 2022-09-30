using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public static class MsSqlConfigurationExtension
{
    public static IConfigurationBuilder AddMsSqlConfiguration(
        this IConfigurationBuilder builder,
        string dbConnectionString)
    {
        return builder.Add(new MsSqlConfigurationSource(dbConnectionString));
    }
}