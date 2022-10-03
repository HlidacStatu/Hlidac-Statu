using System;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.LibCore.ConfigurationProviders;

public class MsSqlConfigurationContext : DbContext
{
    private readonly string _connectionString;
    
    public MsSqlConfigurationContext(string connectionString) =>
        _connectionString = connectionString;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new ArgumentException($"Connection string for {nameof(MsSqlConfigurationContext)} is not set.",
                nameof(_connectionString));

        optionsBuilder.UseSqlServer(_connectionString);
    }
    
    public DbSet<MsSqlConfigurationValue> ConfigurationValues { get; set; }
}