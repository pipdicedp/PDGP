using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TradeLicence.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || (Request.Headers["Accept"].ToString().Contains("application/json")
                    && !Request.Headers["Accept"].ToString().Contains("text/html"));
        }

        // Reached via app.UseExceptionHandler("/Error/Index") — catches any
        // unhandled exception thrown anywhere in the request pipeline.
        [Route("Error/Index")]
        public IActionResult Index()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                _logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on {Path}", exceptionFeature.Path);
            }

            if (IsAjaxRequest())
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Something went wrong on our end. Please try again."
                });
            }

            var model = new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(model);
        }

        // Reached via app.UseStatusCodePagesWithReExecute — catches 404, 403, etc.
        [Route("Error/StatusCode/{code:int}")]
        public IActionResult StatusCodeHandler(int code)
        {
            if (IsAjaxRequest())
            {
                return StatusCode(code, new
                {
                    success = false,
                    error = $"Request failed (HTTP {code})."
                });
            }

            var model = new ErrorViewModel
            {
                StatusCode = code,
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View("StatusCode", model);
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}