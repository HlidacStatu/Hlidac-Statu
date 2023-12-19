using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Serilog;

namespace HlidacStatu.Web.Views.Shared.Components;

public class AutocompleteErrorLogger : IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        Log.ForContext<AutocompleteErrorLogger>().Error(exception, "During autocomplete usage an error occured.");
        return ValueTask.CompletedTask;
    }
}