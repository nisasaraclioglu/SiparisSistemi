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

        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction("CustomerHome");
        }

        [HttpGet]
        public async Task<IActionResult> CustomerHome()
        {
            try
            {
                var products = await _context.Products
                    .Select(p => new ProductViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Stock = p.Stock,
                        ProductType = p.ProductType,
                        ImageUrl = !string.IsNullOrEmpty(p.ImagePath) 
                            ? p.ImagePath.StartsWith("/") 
                                ? p.ImagePath 
                                : $"/images/products/{p.ImagePath}"
                            : "/images/products/default.jpg"
                    })
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ürünler yüklenirken hata: {ex.Message}");
                return View(new List<ProductViewModel>());
            }
        }

        [HttpGet]
        public IActionResult Account()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");

            if (customerId == null)
            {
                return RedirectToAction("CustomerLogin", "Login");
            }

            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);

            if (customer == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            return View(customer);
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
                    ProductType = p.ProductType,
                    ImageUrl = string.IsNullOrEmpty(p.ImagePath) 
                        ? "/images/products/default.jpg" 
                        : p.ImagePath
                })
                .ToListAsync();

            return PartialView("_ProductGrid", products);
        }

        [HttpPost]
        public async Task<JsonResult> AddToCart(int productId, int quantity)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { success = false, message = "Lütfen önce giriş yapın." });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                var order = new Orders
                {
                    CustomerID = customerId.Value,
                    ProductID = productId,
                    Quantity = quantity,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending",
                    TotalPrice = quantity * product.Price
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ürün sepete eklendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ApproveAllCart()
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { success = false, message = "Lütfen giriş yapın." });
                }

                // Bekleyen siparişleri al
                var pendingOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId && o.OrderStatus == "Pending")
                    .ToListAsync();

                if (!pendingOrders.Any())
                {
                    return Json(new { success = false, message = "Onaylanacak sipariş bulunamadı." });
                }

                // Tüm bekleyen siparişleri güncelle
                foreach (var order in pendingOrders)
                {
                    order.OrderStatus = "AwaitingApproval";
                    _context.Orders.Update(order);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Sepetiniz admin onayına gönderildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ApproveOrder(int orderId)
        {
            try
            {
                var order = _context.Orders
                    .Include(o => o.Product)
                    .FirstOrDefault(o => o.OrderID == orderId && o.OrderStatus == "AwaitingApproval");

                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş bulunamadı veya zaten tamamlanmış." });
                }

                if (order.Product.Stock < order.Quantity)
                {
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                // Stok azaltma
                order.Product.Stock -= order.Quantity;

                // Sipariş durumunu güncelle
                order.OrderStatus = "Completed";

                _context.SaveChanges(); // Değişiklikleri kaydet
                return Json(new { success = true, message = "Sipariş onaylandı ve tamamlandı." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApproveOrder Error: {ex.Message}");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Orders()
        {
            // Session'dan CustomerID'yi al
            var customerId = HttpContext.Session.GetInt32("CustomerID");

            if (customerId == null)
            {
                return RedirectToAction("CustomerLogin", "Login");
            }

            // Siparişleri veritabanından çek
            var orders = _context.Orders
                .Where(o => o.CustomerID == customerId)
                .Include(o => o.Product) // Ürün bilgilerini dahil et
                .ToList();

            if (orders == null || !orders.Any())
            {
                return View(new List<Orders>()); // Boş liste döndür
            }

            return View(orders);
        }
    }
}
