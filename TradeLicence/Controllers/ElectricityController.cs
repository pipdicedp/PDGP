using Microsoft.AspNetCore.Mvc;

namespace TradeLicence.Controllers
{
    public class ElectricityController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
