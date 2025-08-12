using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace PlatyUredniku.DataTable;

public sealed class DataTableFilters
{
    public interface IFilterable
    {
        string Key { get; }
        string Label { get; }
        
        // method for rendering filter to HTML 
        IHtmlContent Render();
    }

    public abstract class FilterField : IFilterable
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public abstract IHtmlContent Render();
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
                         <div class="row g-2 align-items-center">
                           <div class="col">
                             <label class="form-label mb-1" for="{{htmlEncoder.Encode($"{Key}From")}}">od {{htmlEncoder.Encode(Unit ?? "")}}</label>
                             <input class="form-control form-control-sm" type="number"
                                    id="{{htmlEncoder.Encode($"{Key}From")}}"
                                    name="{{htmlEncoder.Encode($"{Key}From")}}"
                                    min="{{RenderDecimal(Min)}}"
                                    max="{{RenderDecimal(Max)}}"{{stepAttr}}{{fromValAttr}}>
                           </div>
                           <div class="col">
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
}