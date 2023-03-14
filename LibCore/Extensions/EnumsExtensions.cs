using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HlidacStatu.LibCore.Extensions;

public static class EnumsExtensions
{
    public static string? GetEnumDisplayName(this Enum enumValue)
    {
        var type = enumValue.GetType();
        var memInfo = type.GetMember(enumValue.ToString()).First();
        var attributes = memInfo.GetCustomAttributes(typeof(DisplayAttribute), false);
        var attribute = (DisplayAttribute?)attributes.FirstOrDefault();
        return attribute == null ? enumValue.ToString() : attribute.Name;
    }
    
    public static string? GetEnumDisplayDescription(this Enum enumValue)
    {
        var type = enumValue.GetType();
        var memInfo = type.GetMember(enumValue.ToString()).First();
        var attributes = memInfo.GetCustomAttributes(typeof(DisplayAttribute), false);
        var attribute = (DisplayAttribute?)attributes.FirstOrDefault();
        return attribute == null ? enumValue.ToString() : attribute.Description;
    }

    public static bool NotIn<T>(this Enum? enumValue, params T[] args) where T: Enum
    {
        if (enumValue is null)
            return false;
        return !args.Any(a => a.Equals((T)enumValue));
    }
}