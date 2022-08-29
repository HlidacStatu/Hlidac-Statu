using Microsoft.AspNetCore.Mvc.Rendering;

namespace VolicskyPrukaz.Helpers;

public static class TagHelpers
{
    public static string NavButton(this IHtmlHelper htmlHelper, string page)
    {
        var routePage = htmlHelper.ViewContext.RouteData.Values["page"]?.ToString();

        var returnActive = string.Equals(routePage, $"/{page}", StringComparison.InvariantCultureIgnoreCase);

        return returnActive ? "btn-secondary" : "btn-primary";
    } 
}