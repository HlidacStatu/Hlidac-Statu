using Microsoft.AspNetCore.Components.Web;
using Serilog;
using System;
using System.Threading.Tasks;

namespace PlatyUredniku.Views.Shared.Components;

public class AutocompleteErrorLogger : IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        Log.ForContext<AutocompleteErrorLogger>().Error(exception, "During autocomplete usage an error occured.");
        return ValueTask.CompletedTask;
    }
}