using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Ui.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
