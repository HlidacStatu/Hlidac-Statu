using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;

    public MsSqlConfigurationSource(string connectionString) =>
        _connectionString = connectionString;

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new MsSqlConfigurationProvider(_connectionString);
}