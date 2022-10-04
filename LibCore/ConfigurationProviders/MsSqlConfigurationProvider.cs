using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;
    private readonly string? _tag;
    private readonly string _environment;

    public MsSqlConfigurationProvider(string connectionString, string environment, string? tag)
    {
        _connectionString = connectionString;
        _tag = tag;
        _environment = environment;
    }

    public override void Load()
    {
        // Pokud existuje tag, tak se vybere primárně hodnota tagovaná
        // a v případě, že neexistuje tagovaná hodnota klíče, vybere se s hodnotou null ve sloupci tag
        // Pokud neexistuje tag, tak se vybere pouze hodnota s null ve sloupci tag
        
        using var dbContext = new MsSqlConfigurationContext(_connectionString);

        var tagless = dbContext.ConfigurationValues
            .Where(c => c.Environment == _environment)
            .Where(c => c.Tag == null)
            .ToDictionary(c => c.KeyName, c => c.KeyValue, StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(_tag))
        {
            Data = tagless;
            return;
        }
        
        var tags = dbContext.ConfigurationValues
            .Where(c => c.Environment == _environment)
            .Where(c => c.Tag == _tag)
            .ToDictionary(c => c.KeyName, c => c.KeyValue, StringComparer.OrdinalIgnoreCase);

        foreach (var record in tagless)
        {
            if (!tags.ContainsKey(record.Key))
            {
                tags.Add(record.Key, record.Value);
            }
        }

        Data = tags;

    }
    
    

}