using System;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Entities;

namespace HlidacStatu.Repositories;

public static class ConfigurationValueRepo
{
    public static async Task<List<ConfigurationValue>> GetConfigurationValues()
    {
        await using var dbContext = new DbEntities();

        var confValues = dbContext.ConfigurationValues.AsNoTracking();
        return await confValues.ToListAsync();
    }

    public static async Task<ConfigurationValue> Find(ConfigurationValue configurationValue)
    {
        if (configurationValue is null)
            return default;

        await using var dbContext = new DbEntities();
        return await dbContext.ConfigurationValues.AsNoTracking()
            .Where(cv => cv.KeyName == configurationValue.KeyName)
            .Where(cv => cv.KeyValue == configurationValue.KeyValue)
            .Where(cv => cv.Environment == configurationValue.Environment)
            .Where(cv => cv.Tag == configurationValue.Tag)
            .SingleOrDefaultAsync();
    }

    public static async Task UpsertKey(ConfigurationValue configurationValue)
    {
        await using var dbContext = new DbEntities();
        throw new NotImplementedException("musime to dokoncit - podzemkastav");

    }

}