using System;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.LibCore;
using HlidacStatu.Web.Framework;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/{action}/{_id?}/{_dataid?}")]
    public partial class ApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiController(UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Autocomplete(string q, string category, CancellationToken ctx)
        {
            var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");
            var autocompletePath = $"/autocomplete/autocomplete?q={q}";
            var cat = string.IsNullOrWhiteSpace(category) ? "" : $"&category={category}";
            var uri = new Uri($"{autocompleteHost}{autocompletePath}{cat}");
            using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClient);

            try
            {
                var response = await client.GetAsync(uri, ctx);

                return new HttpResponseMessageActionResult(response);
            }
            catch (Exception ex) when ( ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // canceled by user
                Util.Consts.Logger.Info("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Warning("Autocomplete API problem.", e, new { q });
            }
            
            return NoContent();
            
        }

        // GET: ApiV1
        public ActionResult Index()
        {
            return RedirectToAction("Index", "ApiV1");

        }

    }
}