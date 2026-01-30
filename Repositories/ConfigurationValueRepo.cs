using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public static class ConfigurationValueRepo
{
    public static async Task<List<ConfigurationValue>> GetConfigurationValuesAsync()
    {
        await using var dbContext = new DbEntities();

        var confValues = dbContext.ConfigurationValues.AsNoTracking();
        return await confValues.ToListAsync();
    }

    public static async Task UpsertAsync(ConfigurationValue configurationValue)
    {
        await using var dbContext = new DbEntities();

        if (configurationValue.Id == 0)
        {
            //insert
            dbContext.ConfigurationValues.Add(configurationValue);
        }
        else
        {
            var existingConfig =
                await dbContext.ConfigurationValues.FirstOrDefaultAsync(q => q.Id == configurationValue.Id);

            existingConfig.Tag = configurationValue.Tag;
            existingConfig.Environment = configurationValue.Environment;
            existingConfig.KeyValue = configurationValue.KeyValue;
            existingConfig.KeyName = configurationValue.KeyName;
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task DeleteAsync(ConfigurationValue configurationValue)
    {
        if (configurationValue is null)
            return;
        
        await using var dbContext = new DbEntities();
        dbContext.ConfigurationValues.Remove(configurationValue);
        await dbContext.SaveChangesAsync();

    }

    public static async Task<(int productionKeyCount, int stageKeyCount)> GetStatisticsAsync()
    {
        await using var dbContext = new DbEntities();
        var productionKeyCount =
            await dbContext.ConfigurationValues.CountAsync(cv =>
                cv.Environment == ConfigurationValue.Environments.Production);
        var stageKeyCount =
            await dbContext.ConfigurationValues.CountAsync(cv =>
                cv.Environment == ConfigurationValue.Environments.Stage);

        return (productionKeyCount, stageKeyCount);
    }
}