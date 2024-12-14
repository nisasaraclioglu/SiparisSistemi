using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiparisSistemi.Models;

namespace SiparisSistemi.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // Müşteri anasayfasına yönlendir
            return RedirectToAction("CustomerHome");
        }

        public async Task<IActionResult> CustomerHome()
        {
            try
            {
                // Tüm ürünleri getir
                var products = await _context.Products
                    .Select(p => new ProductViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Stock = p.Stock,
                        ImageUrl = "/images/products/default.jpg" // Varsayılan resim
                    })
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yap
                System.Diagnostics.Debug.WriteLine($"Ürünler getirilirken hata: {ex.Message}");
                return View(new List<ProductViewModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            var products = await _context.Products
                .Where(p => p.ProductName.Contains(searchTerm))
                .Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrl = "/images/products/default.jpg"
                })
                .ToListAsync();

            return PartialView("_ProductGrid", products);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                // Stok kontrolü
                if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                // TODO: Sepete ekleme işlemi
                // Şimdilik başarılı döndürelim
                return Json(new { success = true, message = "Ürün sepete eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu." });
            }
        }
    }
}
