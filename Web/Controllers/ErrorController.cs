using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("error")]
    public class ErrorController : Controller
    {
        private readonly ILogger _logger = Log.ForContext<ErrorController>();
        [Route("500")]
        public IActionResult ApplicationError()
        {
            return View();
        }

        [Route("404")]
        public IActionResult PageNotFound()
        {
            return View();
        }

        [Route("{code:int}")]
        public IActionResult GeneralError(int code)
        {
            return View(code);
        }

        public IActionResult Bot()
        {
            return View();
        }
    }
}