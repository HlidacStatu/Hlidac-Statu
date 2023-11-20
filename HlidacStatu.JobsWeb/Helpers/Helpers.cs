using Devmasters;

namespace WatchdogAnalytics.Helpers;

public static class Helpers
{
    public static string MakeIdFromName(this string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
        
        return name.RemoveDiacritics()
            .Replace(" ", "_")
            .Replace(".", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .ToLower();
    }
}