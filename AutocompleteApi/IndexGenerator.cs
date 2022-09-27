using System;
using System.IO;
using System.Threading.Tasks;
using HlidacStatu.AutocompleteApi.Services;
using Serilog;

namespace HlidacStatu.AutocompleteApi;

public static class IndexGenerator
{
    public static async Task GenerateIndexesAsync(ILogger logger)
    {
        Console.WriteLine("Starting to generate Indexes");
        string folder = "AutocompleteBasicIndexes";
        
        Console.WriteLine($"Getting ready the destination directory [{folder}]");
        if(Directory.Exists(folder))
            Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);

        var adresyIndex = CacheIndexFactory.CreateAdresyCachedIndex(folder, logger);
        var adresyTcs = new TaskCompletionSource<string>();
        adresyIndex.CacheInitFinished += (_, message) => adresyTcs.SetResult(message);
        
        var companyIndex = CacheIndexFactory.CreateCompanyCachedIndex(folder, logger);
        var companyTcs = new TaskCompletionSource<string>();
        companyIndex.CacheInitFinished += (_, message) => companyTcs.SetResult(message);
        
        var kindexIndex = CacheIndexFactory.CreateKindexCachedIndex(folder, logger);
        var kindexTcs = new TaskCompletionSource<string>();
        kindexIndex.CacheInitFinished += (_, message) => kindexTcs.SetResult(message);
        
        var uptimeServerIndex = CacheIndexFactory.CreateUptimeServerCachedIndex(folder, logger);
        var uptimeServerTcs = new TaskCompletionSource<string>();
        uptimeServerIndex.CacheInitFinished += (_, message) => uptimeServerTcs.SetResult(message);

        // do first four indexes at the same time - they are relatively small and easy
        var res = await Task.WhenAll(adresyTcs.Task, companyTcs.Task, kindexTcs.Task, uptimeServerTcs.Task);
        foreach (var m in res)
        {
            Console.WriteLine(m);
        }

        //free memory
        adresyIndex = null;
        companyIndex = null;
        kindexIndex = null;
        uptimeServerIndex = null;

        // do this index separately - might be problematic
        var fullAutocompleteIndex = CacheIndexFactory.CreateFullAutocompleteCachedIndex(folder, logger);
        var fullAutocompleteTcs = new TaskCompletionSource<string>();
        fullAutocompleteIndex.CacheInitFinished += (_, message) => fullAutocompleteTcs.SetResult(message);
        var autocompleteResult = await fullAutocompleteTcs.Task;
        Console.WriteLine(autocompleteResult);
        
        Console.WriteLine("Indexes were generated");

    }
}