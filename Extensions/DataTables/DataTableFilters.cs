using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace HlidacStatu.Extensions.DataTables;

public sealed class DataTableFilters
{
    public interface IFilterable
    {
        string Key { get; }
        string Label { get; }

        bool Hidden { get; } 
        OpenStatus Open { get; }

        // method for rendering filter to HTML 
        IHtmlContent Render();
        Dictionary<string, object?> GetInitialValues();

    }
    public enum OpenStatus
    {
        Auto,
        Open,
        Closed
    }

    public abstract class FilterField : IFilterable
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public bool Hidden { get; init; } = false;
        public OpenStatus Open { get; init; } = OpenStatus.Auto;

        public abstract IHtmlContent Render();
        public abstract Dictionary<string, object?> GetInitialValues();
    }

    public sealed class FilterOption
    {
        public required string Value { get; init; }
        public string? Label { get; init; }
    }

    public sealed class ChoiceFilterField : FilterField
    {
        public bool Multiple { get; init; } = false;
        public required List<FilterOption> Options { get; init; }
        public string[]? Initial { get; init; }

        public override IHtmlContent Render()
        {
            var htmlEncoder = HtmlEncoder.Default;
            var htmlContentBuilder = new HtmlContentBuilder();
            var initial = new HashSet<string>(Initial ?? [], StringComparer.InvariantCultureIgnoreCase);

            foreach (var opt in Options)
            {
                var value = opt.Value ?? string.Empty;
                var label = opt.Label ?? value;
                var id = $"{Key}_{value}";

                htmlContentBuilder.AppendHtml($$"""
                                                <div class="form-check">
                                                  <input class="form-check-input" type="{{(Multiple ? "checkbox" : "radio")}}"
                                                         name="{{htmlEncoder.Encode(Key)}}"
                                                         id="{{htmlEncoder.Encode(id)}}"
                                                         value="{{htmlEncoder.Encode(value)}}"{{(initial.Contains(value) ? " checked" : "")}}>
                                                  <label class="form-check-label" for="{{htmlEncoder.Encode(id)}}">{{htmlEncoder.Encode(label)}}</label>
                                                </div>
                                                """);
            }

            return htmlContentBuilder;
        }
        
        public override Dictionary<string, object?> GetInitialValues()
        {
            var values = new Dictionary<string, object?>();
            if (Initial is not null)
            {
                values[Key] = Initial;
            }
            return values;
        }
    }

    public sealed class RangeFilterField : FilterField
    {
        public decimal Min { get; init; }
        public decimal Max { get; init; }
        public decimal? Step { get; init; }
        public (decimal From, decimal To)? Initial { get; init; }
        public string? Unit { get; init; }

        public override IHtmlContent Render()
        {
            var htmlEncoder = HtmlEncoder.Default;

            var from = Initial?.From;
            var to = Initial?.To;

            var stepAttr = Step.HasValue ? $@" step=""{RenderDecimal(Step.Value)}""" : string.Empty;
            var fromValAttr = from.HasValue ? $@" value=""{RenderDecimal(from.Value)}""" : string.Empty;
            var toValAttr = to.HasValue ? $@" value=""{RenderDecimal(to.Value)}""" : string.Empty;

            var html = $$"""
                         <div class="g-2 align-items-center">
                           <div>
                             <label class="form-label mb-1" for="{{htmlEncoder.Encode($"{Key}From")}}">od {{htmlEncoder.Encode(Unit ?? "")}}</label>
                             <input class="form-control form-control-sm" type="number"
                                    id="{{htmlEncoder.Encode($"{Key}From")}}"
                                    name="{{htmlEncoder.Encode($"{Key}From")}}"
                                    min="{{RenderDecimal(Min)}}"
                                    max="{{RenderDecimal(Max)}}"{{stepAttr}}{{fromValAttr}}>
                           </div>
                           <div>
                             <label class="form-label mb-1" for="{{htmlEncoder.Encode($"{Key}To")}}">do {{htmlEncoder.Encode(Unit ?? "")}}</label>
                             <input class="form-control form-control-sm" type="number"
                                    id="{{htmlEncoder.Encode($"{Key}To")}}"
                                    name="{{htmlEncoder.Encode($"{Key}To")}}"
                                    min="{{RenderDecimal(Min)}}"
                                    max="{{RenderDecimal(Max)}}"{{stepAttr}}{{toValAttr}}>
                           </div>
                         </div>
                         """;

            return new HtmlString(html);
        }
        
        public override Dictionary<string, object?> GetInitialValues()
        {
            var values = new Dictionary<string, object?>();
            if (Initial.HasValue)
            {
                values[Key + "From"] = Initial.Value.From;
                values[Key + "To"] = Initial.Value.To;
            }
            return values;
        }
    }

    public sealed class TextFilterField : FilterField
    {
        public string? Placeholder { get; init; }
        public string? Initial { get; init; }

        public override IHtmlContent Render()
        {
            var htmlEncoder = HtmlEncoder.Default;

            var placeholderAttr = !string.IsNullOrWhiteSpace(Placeholder)
                ? $@" placeholder=""{htmlEncoder.Encode(Placeholder!)}"""
                : string.Empty;

            var valueAttr = !string.IsNullOrEmpty(Initial)
                ? $@" value=""{htmlEncoder.Encode(Initial!)}"""
                : string.Empty;

            var html = $$"""
                         <input class="form-control form-control-sm" type="search"
                                name="{{htmlEncoder.Encode(Key)}}"{{placeholderAttr}}{{valueAttr}}>
                         """;

            return new HtmlString(html);
        }
        
        public override Dictionary<string, object?> GetInitialValues()
        {
            var values = new Dictionary<string, object?>();
            if (Initial is not null)
            {
                values[Key] = Initial;
            }
            return values;
        }
    }
    
    private static string RenderDecimal(decimal v) => v.ToString(CultureInfo.InvariantCulture);
}

public sealed class DataTableFilter
{
    public required IReadOnlyList<DataTableFilters.IFilterable> Filters { get; init; }
    public required string DataEndpointUrl { get; init; }
    public object? InitialData { get; init; }
    public string FormId { get; init; } = "filterForm";
    public string TableId { get; init; } = "myFilteredTable";

    /// <summary>
    /// Use format like
    /// `"[[3, 'desc']]"`
    /// </summary>
    public string DefaultOrder { get; init; } = "[]";
    
    public string GetInitialValuesJson()
    {
        var initialValues = new Dictionary<string, object?>();

        foreach (var filter in Filters)
        {
            // Sloučí hodnoty z jednotlivých filtrů
            foreach (var kvp in filter.GetInitialValues())
            {
                initialValues[kvp.Key] = kvp.Value;
            }
        }

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Serialize(initialValues, jsonOptions);
    }
    
    public string GetFilterOptionsJson()
    {
        var filterOptions = new Dictionary<string, object>();
        
        foreach (var filter in Filters)
        {
            if (filter is DataTableFilters.ChoiceFilterField choiceFilter)
            {
                var optionsMap = new Dictionary<string, string>();
                foreach (var option in choiceFilter.Options)
                {
                    optionsMap[option.Value] = option.Label ?? option.Value;
                }
                filterOptions[choiceFilter.Key] = optionsMap;
            }
        }
        
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Serialize(filterOptions, jsonOptions);
    }
    
    public IHtmlContent RenderResetButton()
    {
        var initialValuesJson = GetInitialValuesJson();

        var buttonHtml = $"""
                          <div class="d-grid gap-2">
                            <button type="button" class="btn btn-hs btn-sm btn-secondary mt-3" id="resetFiltersButton" data-init='{HtmlEncoder.Default.Encode(initialValuesJson)}'>
                              Resetovat filtry
                            </button>
                          </div>
                          """;

        return new HtmlString(buttonHtml);
    }
}