using Microsoft.AspNetCore.Mvc;

namespace TradeLicence.Controllers
{
    public class WaterController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
