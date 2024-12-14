using Microsoft.AspNetCore.Mvc;
using SiparisSistemi.Models;

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
        public IActionResult AdminLogin(string username, decimal passwordHash)
        {
            // Admin işlevselliğini daha sonra ekleyeceğiz
            return RedirectToAction("CustomerLogin");
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
            var customers = _context.Customers.FirstOrDefault(c => c.CustomerName == CustomerName && c.PasswordHash == PasswordHash);

            if (customers != null)
            {
                // Müşteri Dashboard'a yönlendirme
                return RedirectToAction("Dashboard", "Customer");
            }

            ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        // Müşteri Kayıt GET Metodu
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Müşteri Kayıt POST Metodu
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

                    try
                    {
                        // Varsayılan değerleri ayarla
                        customer.Budget = 0;
                        customer.CustomerType = "Standard";
                        customer.TotalSpent = 0;
                        customer.Orders = new List<Orders>();
                        customer.Logs = new List<Logs>();

                        _context.Customers.Add(customer);
                        var result = _context.SaveChanges();
                        
                        System.Diagnostics.Debug.WriteLine($"Kayıt sonucu: {result} satır etkilendi");
                        
                        TempData["SuccessMessage"] = "Kayıt başarıyla oluşturuldu!";
                        return RedirectToAction("CustomerLogin");
                    }
                    catch (Exception dbEx)
                    {
                        // Veritabanı işlemi sırasındaki hatayı detaylı logla
                        System.Diagnostics.Debug.WriteLine($"Veritabanı hatası: {dbEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"İç hata: {dbEx.InnerException?.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {dbEx.StackTrace}");
                        
                        ModelState.AddModelError("", "Veritabanına kayıt sırasında bir hata oluştu.");
                        return View(customer);
                    }
                }

                // ModelState geçerli değilse hataları logla
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Model hatası: {modelError.ErrorMessage}");
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                // Genel hata mesajını logla
                System.Diagnostics.Debug.WriteLine($"Genel hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"İç hata: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                ModelState.AddModelError("", "Kayıt sırasında beklenmeyen bir hata oluştu.");
                return View(customer);
            }
        }
    }
}
