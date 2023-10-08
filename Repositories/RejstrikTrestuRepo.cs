using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

namespace HlidacStatu.Repositories;

public static class RejstrikTrestuRepo
{
    private static string _indexName = "rejstrik-trestu-pravnickych-osob";
    private static Manager.IndexType _indexType = Manager.IndexType.DataSource;

    public static async Task<List<RejstrikTrestu>> FindTrestyAsync(string ico)
    {
        var client = await Manager.GetESClientAsync(_indexName, idxType: _indexType);
        Nest.ISearchResponse<RejstrikTrestu> result;
        try
        {
            result = await client.SearchAsync<RejstrikTrestu>(s => s
                .Query(q => q
                    .Match(m=>m
                        .Field(f=>f.ICO)
                        .Query(ico)
                        )
                    )
            );

            var res = result.Documents.ToList();
            return res;
        }
        catch (Exception e)
        {
            // ignored
        }

        return new List<RejstrikTrestu>();
    }
    
    public static async Task<RejstrikTrestu> FindTrestByIdAsync(string id)
    {
        var client = await Manager.GetESClientAsync(_indexName, idxType: _indexType);

        try
        {
            var response = await client.GetAsync<RejstrikTrestu>(id);

            return response.IsValid
                ? response.Source
                : null;
        }
        catch (Exception e)
        {
            // ignored
        }

        return null;
    }
    
    // public static async MonitoredTask SaveAsync(RejstrikTrestu rejstrikTrestu)
    // {
    //     if (rejstrikTrestu == null) throw new ArgumentNullException(nameof(rejstrikTrestu));
    //     
    //     
    //     var client = await Manager.GetESClientAsync(_indexName, idxType: _indexType);
    //
    //     var res = await client.IndexAsync(rejstrikTrestu, o => o.Id(rejstrikTrestu.Id));
    //     if (!res.IsValid)
    //     {
    //         throw new ApplicationException(res.ServerError?.ToString());
    //     }
    // }
    
    
    
}