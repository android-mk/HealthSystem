using Microsoft.AspNetCore.Mvc;

namespace HealthSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}