using Microsoft.AspNetCore.Mvc;

namespace TradeLicence.Controllers
{
    // Serves the public Guidance Puducherry landing page. This is the very
    // first page the application shows (see the default route in
    // Program.cs). It carries its own self-contained header/nav/footer
    // markup, so it renders with its own layout (_PYGuidanceLayout) rather
    // than the investor-portal _Layout used by the rest of the site.
    public class PYGuidancehomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
