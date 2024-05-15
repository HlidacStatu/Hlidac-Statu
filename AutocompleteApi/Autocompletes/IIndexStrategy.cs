using System;
using System.Collections.Generic;

namespace HlidacStatu.AutocompleteApi.Autocompletes;

public interface IIndexStrategy: IDisposable
{
    int Count();
    IEnumerable<object>? Search(string query, int numResults = 10, string? filter = null);
    bool IsCorrupted();
}