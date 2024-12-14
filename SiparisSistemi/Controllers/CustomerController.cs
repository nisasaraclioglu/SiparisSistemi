using Microsoft.AspNetCore.Mvc;

namespace SiparisSistemi.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
