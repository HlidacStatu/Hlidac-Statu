//Moved to HlidacStatu.LibCore.Filters

// using Microsoft.OpenApi.Models;
//
// using Swashbuckle.AspNetCore.SwaggerGen;
//
// using System.Collections.Generic;
//
//
// namespace HlidacStatu.Web.Filters
// {
//     public class AddApiAuthHeaderParameter : IOperationFilter
//     {
//         public void Apply(OpenApiOperation operation, OperationFilterContext context)
//         {
//             if (operation.Security == null)
//                 operation.Security = new List<OpenApiSecurityRequirement>();
//
//
//             var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "apiKey" } };
//             operation.Security.Add(new OpenApiSecurityRequirement
//             {
//                 [scheme] = new List<string>()
//             });
//         }
//     }
// }
