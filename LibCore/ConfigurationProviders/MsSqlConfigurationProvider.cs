using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;

    public MsSqlConfigurationProvider(string connectionString) =>
        _connectionString = connectionString;

    public override void Load()
    {
        using var dbContext = new MsSqlConfigurationContext(_connectionString);

        Data = dbContext.ConfigurationValues.ToDictionary(c => c.Id, c => c.Value, StringComparer.OrdinalIgnoreCase);
    }

}