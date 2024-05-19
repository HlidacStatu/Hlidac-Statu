using System;
using System.Collections.Generic;
using System.Linq;
using Whisperer;

namespace HlidacStatu.AutocompleteApi.Autocompletes;

public class IndexStrategy<T> : IIndexStrategy where T : IEquatable<T>
{
    private readonly Index<T> _index;

    private readonly Func<IEnumerable<ScoredResult<T>>, IEnumerable<ScoredResult<T>>> _orderFunction;
    
    public IndexStrategy(string path, Func<IEnumerable<ScoredResult<T>>, IEnumerable<ScoredResult<T>>> orderFunction)
    {
        _orderFunction = orderFunction;
        _index = new Index<T>(path);
    }
    
    public void Dispose() => _index.Dispose();

    public int Count() => _index.Count();

    public IEnumerable<object>? Search(string query, int numResults = 10, string? filter = null)
    {
        var results = _index.Search(query, numResults * 2, filter);

        return _orderFunction(results)
            .Take(numResults)
            .Select(s => s.Document) as IEnumerable<object>;
    }

    public bool IsCorrupted()
    {
        try
        {
            _index.Search("a");
            return false;
        }
        catch
        {
            return true;
        }
    }
}