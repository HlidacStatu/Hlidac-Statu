using System;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.LibCore;
using HlidacStatu.Web.Framework;
using Serilog;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/{action}/{_id?}/{_dataid?}")]
    public partial class ApiController : Controller
    {
        private readonly ILogger _logger = Log.ForContext<ApiController>();
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
                _logger.Information("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                _logger.Warning(e, "Autocomplete API problem.", new { q });
            }
            
            return NoContent();
            
        }

        public async Task<IActionResult> Autocomplete2(string q, string category, CancellationToken ctx)
        {
            var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint").Replace("20002","20003");
            var autocompletePath = $"/autocomplete/autocomplete?q={q}";
            var cat = string.IsNullOrWhiteSpace(category) ? "" : $"&category={category}";
            var uri = new Uri($"{autocompleteHost}{autocompletePath}{cat}");
            using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClient);

            try
            {
                var response = await client.GetAsync(uri, ctx);

                return new HttpResponseMessageActionResult(response);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // canceled by user
                _logger.Information("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                _logger.Warning(e, "Autocomplete API problem.", new { q });
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