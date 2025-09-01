using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{


    public static class FeedbackModelTagHelperExtensions
    {
        public static IServiceCollection FeedbackModelTagHelper(this IServiceCollection services)
        {

            // Ensure controllers from this library are discovered
            services.AddControllers()
                .AddApplicationPart(Assembly.GetExecutingAssembly())
                .AddControllersAsServices();

            return services;
        }
    }
}
