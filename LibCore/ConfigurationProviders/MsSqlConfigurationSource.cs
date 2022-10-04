using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    private readonly string _environment;
    private readonly string? _tag;

    public MsSqlConfigurationSource(string connectionString, string environment, string? tag)
    {
        _connectionString = connectionString;
        _environment = environment;
        _tag = tag;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new MsSqlConfigurationProvider(_connectionString, _environment, _tag);
}