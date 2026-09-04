using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Ui.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
