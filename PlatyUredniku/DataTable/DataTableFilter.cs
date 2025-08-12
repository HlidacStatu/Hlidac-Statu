using System.Collections.Generic;

namespace PlatyUredniku.DataTable;

public abstract class FilterField
{
    public required string Key { get; init; }    // machine name, e.g. "gender"
    public required string Label { get; init; }  // UI label, e.g. "Pohlaví"
}

// Single- or multi-choice (radio/checkbox/select[multiple])
public sealed class ChoiceFilterField : FilterField
{
    public bool Multiple { get; init; } = false;                 // false=radio, true=checkbox/multi
    public required List<FilterOption> Options { get; init; }    // available values
    public string[]? Initial { get; init; }                      // preselected values
}

public sealed class FilterOption
{
    public required string Value { get; init; }  // machine value
    public string? Label { get; init; }          // shown label (defaults to Value in UI if null)
}

// Numeric range (int/decimal/double)
public sealed class RangeFilterField : FilterField
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public decimal? Step { get; init; }
    public (decimal From, decimal To)? Initial { get; init; }
    public string? Unit { get; init; }           // e.g. "Kč", "ks"
}

// Free text / search
public sealed class TextFilterField : FilterField
{
    public string? Placeholder { get; init; }
    public string? Initial { get; init; }
}

// -------------------------------
// Model for _SeznamFilter.cshtml
// -------------------------------
public sealed class SeznamFilterModel
{
    public required IReadOnlyList<FilterField> Filters { get; init; }  // schema for the form
    public required string DataEndpointUrl { get; init; }              // AJAX endpoint
    public object? InitialData { get; init; }                          // optional rows for first render
    public string FormId { get; init; } = "filterForm";
    public string TableId { get; init; } = "myFilteredTable";
}
