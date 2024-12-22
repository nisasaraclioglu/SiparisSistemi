using Microsoft.AspNetCore.Mvc;
using SiparisSistemi.Models;
using SiparisSistemi.Helpers;

namespace SiparisSistemi.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin Giriş Metodu
        [HttpPost]
        public IActionResult AdminLogin(string username, string password)
        {
            var hashedPassword = HashHelper.HashPassword(password);
            var admin = _context.Admin.FirstOrDefault(a => 
                a.Username == username && 
                a.PasswordHash == hashedPassword);

            if (admin != null)
            {
                HttpContext.Session.SetInt32("AdminID", admin.AdminID);
                return RedirectToAction("Dashboard", "Admin");
            }

            // Admin değilse müşteri girişini kontrol et
            var customer = _context.Customers.FirstOrDefault(c => 
                c.CustomerName == username && 
                c.PasswordHash == hashedPassword);

            if (customer != null)
            {
                HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                return RedirectToAction("Dashboard", "Customer");
            }

            ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
            return View("CustomerLogin");
        }

        // Müşteri Giriş Metodu
        [HttpGet]
        public IActionResult CustomerLogin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CustomerLogin(string CustomerName, string PasswordHash)
        {
            try
            {
                var hashedPassword = HashHelper.HashPassword(PasswordHash);
                var customer = _context.Customers
                    .FirstOrDefault(c => c.CustomerName == CustomerName);

                if (customer != null && customer.PasswordHash == hashedPassword)
                {
                    HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                    return RedirectToAction("Dashboard", "Customer");
                }

                ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }
            catch (Exception ex)
            {
                // Hata logla
                System.Diagnostics.Debug.WriteLine($"Login hatası: {ex.Message}");
                ViewBag.ErrorMessage = "Giriş sırasında bir hata oluştu.";
                return View();
            }
        }

[HttpGet]
    public IActionResult Logout()
    {
        // Session'ı temizle
        HttpContext.Session.Clear();

        // Giriş sayfasına yönlendir
        return RedirectToAction("CustomerLogin", "Login");
    }

    // Müşteri Kayıt GET Metodu
    [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Aynı kullanıcı adı var mı kontrol et
                    if (_context.Customers.Any(c => c.CustomerName == customer.CustomerName))
                    {
                        ModelState.AddModelError("CustomerName", "Bu kullanıcı adı zaten kullanılıyor.");
                        return View(customer);
                    }

                    // Şifreyi hashle
                    customer.PasswordHash = HashHelper.HashPassword(customer.PasswordHash);

                    // Varsayılan değerleri ayarla
                    customer.CustomerType = "Standard";
                    customer.TotalSpent = 0;
                    customer.Orders = new List<Orders>();
                    customer.Logs = new List<Logs>();

                    _context.Customers.Add(customer);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Kayıt başarıyla oluşturuldu!";
                    return RedirectToAction("CustomerLogin");
                }

                return View(customer);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu.");
                return View(customer);
            }
        }

    }
}
