using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace HlidacStatu.Web.Views.Shared.Components;

public class AutocompleteErrorLogger : IErrorBoundaryLogger
{
    //private Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger<AutocompleteErrorLogger>();

    public ValueTask LogErrorAsync(Exception exception)
    {
        Util.Consts.Logger.Error("During autocomplete usage an error occured.", exception);
        return ValueTask.CompletedTask;
    }
}