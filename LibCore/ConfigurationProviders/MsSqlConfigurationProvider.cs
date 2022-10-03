using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;
    private readonly string _tag;
    private readonly string _environment;

    public MsSqlConfigurationProvider(string connectionString, string environment, string tag)
    {
        _connectionString = connectionString;
        _tag = tag;
        _environment = environment;
    }

    public override void Load()
    {
        using var dbContext = new MsSqlConfigurationContext(_connectionString);

        Data = dbContext.ConfigurationValues
            .Where(c => c.Environment == _environment)
            .Where(c => c.Tag == _tag) //fix query
            .ToDictionary(c => c.KeyName, c => c.KeyValue, StringComparer.OrdinalIgnoreCase);
    }

}