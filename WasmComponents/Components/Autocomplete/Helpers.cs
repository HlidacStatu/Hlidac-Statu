namespace WasmComponents.Components.Autocomplete;

public class Helpers
{
    public static (string queryString, string queryTagLengths) CreateQueryWithOffsets(List<string> input)
    {
        string qs = "";
        string qtl = "";

        for (var index = 0; index < input.Count; index++)
        {
            var item = input[index];

            if (index == input.Count - 1)
            {
                qtl += $"{item.Length}";
                qs += $"{item}";
            }
            else
            {
                qtl += $"{item.Length},";
                qs += $"{item} ";
            }
        }

        return (qs, qtl);
    }

    public static List<string> ParseQueryStringWithOffsets(string queryString, string lengthsString)
    {
        if (string.IsNullOrWhiteSpace(queryString) || string.IsNullOrWhiteSpace(lengthsString))
            return null;

        var offsets = lengthsString.Split(",",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        List<string> results = new();

        int previousOffset = 0;
        foreach (var offsetText in offsets)
        {
            if (!int.TryParse(offsetText, out int tagLength))
                continue;

            results.Add(queryString.Substring(previousOffset, tagLength));

            // +1 here is a space between tags
            previousOffset += tagLength + 1;
        }

        return results;
    }

    public static List<string> ParseQueryStringWithoutOffsets(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return null;

        return queryString.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}