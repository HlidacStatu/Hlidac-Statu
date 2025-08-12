// IFilterable + concrete fields (put in a shared UI folder/namespace)

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PlatyUredniku.DataTable;

public interface IFilterable
{
    string Key { get; }
    string Label { get; }
    IHtmlContent Draw(); // renders the inner form controls (no accordion markup)
}

public abstract class FilterField : IFilterable
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public abstract IHtmlContent Draw();
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

    public override IHtmlContent Draw()
    {
        var initial = new HashSet<string>(Initial ?? Array.Empty<string>(), StringComparer.InvariantCultureIgnoreCase);
        var builder = new HtmlContentBuilder();

        foreach (var opt in Options)
        {
            var value = opt.Value;
            var label = opt.Label ?? opt.Value;
            var id = $"{Key}_{value}";

            var wrap = new TagBuilder("div");
            wrap.AddCssClass("form-check");

            var input = new TagBuilder("input");
            input.AddCssClass("form-check-input");
            input.Attributes["type"] = Multiple ? "checkbox" : "radio";
            input.Attributes["name"] = Key;
            input.Attributes["id"] = id;
            input.Attributes["value"] = value;
            if (initial.Contains(value))
                input.Attributes["checked"] = "checked";

            var lab = new TagBuilder("label");
            lab.AddCssClass("form-check-label");
            lab.Attributes["for"] = id;
            lab.InnerHtml.Append(label);

            wrap.InnerHtml.AppendHtml(input);
            wrap.InnerHtml.AppendHtml(lab);

            builder.AppendHtml(wrap);
        }

        return builder;
    }
}

public sealed class RangeFilterField : FilterField
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public decimal? Step { get; init; }
    public (decimal From, decimal To)? Initial { get; init; }
    public string? Unit { get; init; }

    public override IHtmlContent Draw()
    {
        string D(decimal v) => v.ToString(CultureInfo.InvariantCulture);
        var from = Initial?.From;
        var to = Initial?.To;

        var row = new TagBuilder("div");
        row.AddCssClass("row g-2 align-items-center");

        // from
        var colFrom = new TagBuilder("div"); colFrom.AddCssClass("col");
        var labFrom = new TagBuilder("label");
        labFrom.AddCssClass("form-label mb-1");
        labFrom.Attributes["for"] = $"{Key}From";
        labFrom.InnerHtml.Append($"od {Unit}");
        var inpFrom = new TagBuilder("input");
        inpFrom.AddCssClass("form-control form-control-sm");
        inpFrom.Attributes["type"] = "number";
        inpFrom.Attributes["id"] = $"{Key}From";
        inpFrom.Attributes["name"] = $"{Key}From";
        inpFrom.Attributes["min"] = D(Min);
        inpFrom.Attributes["max"] = D(Max);
        if (Step.HasValue) inpFrom.Attributes["step"] = D(Step.Value);
        if (from.HasValue) inpFrom.Attributes["value"] = D(from.Value);
        colFrom.InnerHtml.AppendHtml(labFrom);
        colFrom.InnerHtml.AppendHtml(inpFrom);

        // to
        var colTo = new TagBuilder("div"); colTo.AddCssClass("col");
        var labTo = new TagBuilder("label");
        labTo.AddCssClass("form-label mb-1");
        labTo.Attributes["for"] = $"{Key}To";
        labTo.InnerHtml.Append($"do {Unit}");
        var inpTo = new TagBuilder("input");
        inpTo.AddCssClass("form-control form-control-sm");
        inpTo.Attributes["type"] = "number";
        inpTo.Attributes["id"] = $"{Key}To";
        inpTo.Attributes["name"] = $"{Key}To";
        inpTo.Attributes["min"] = D(Min);
        inpTo.Attributes["max"] = D(Max);
        if (Step.HasValue) inpTo.Attributes["step"] = D(Step.Value);
        if (to.HasValue) inpTo.Attributes["value"] = D(to.Value);
        colTo.InnerHtml.AppendHtml(labTo);
        colTo.InnerHtml.AppendHtml(inpTo);

        row.InnerHtml.AppendHtml(colFrom);
        row.InnerHtml.AppendHtml(colTo);

        return row;
    }
}

public sealed class TextFilterField : FilterField
{
    public string? Placeholder { get; init; }
    public string? Initial { get; init; }

    public override IHtmlContent Draw()
    {
        var input = new TagBuilder("input");
        input.AddCssClass("form-control form-control-sm");
        input.Attributes["type"] = "search";
        input.Attributes["name"] = Key;
        if (!string.IsNullOrWhiteSpace(Placeholder))
            input.Attributes["placeholder"] = Placeholder!;
        if (!string.IsNullOrEmpty(Initial))
            input.Attributes["value"] = Initial!;
        return input;
    }
}

public sealed class SeznamFilterModel
{
    public required IReadOnlyList<IFilterable> Filters { get; init; }
    public required string DataEndpointUrl { get; init; }
    public object? InitialData { get; init; }
    public string FormId { get; init; } = "filterForm";
    public string TableId { get; init; } = "myFilteredTable";
}