using Microsoft.AspNetCore.Mvc;

namespace SiparisSistemi.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
