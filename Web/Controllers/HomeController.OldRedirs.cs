using Devmasters;
using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Web.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Datasets;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Web.Filters;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Visit = HlidacStatu.Web.Framework.Visit;


namespace HlidacStatu.Web.Controllers
{
    public partial class HomeController : Controller
    {

        public ActionResult SubjektVazby(string id)
        {
            return RedirectPermanent("/subjekt/vazby/" + id);
        }
    }
}