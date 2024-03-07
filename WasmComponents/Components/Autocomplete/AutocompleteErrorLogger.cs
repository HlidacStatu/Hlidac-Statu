using Microsoft.AspNetCore.Components.Web;

namespace WasmComponents.Components.Autocomplete;

public class AutocompleteErrorLogger : IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        // Log.ForContext<AutocompleteErrorLogger>().Error(exception, "During autocomplete usage an error occured.");
        return ValueTask.CompletedTask;
    }
}