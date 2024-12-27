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

                // Süresi dolmuş beklemedeki siparişleri iptal et
                var now = DateTime.Now;
                var pendingOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId && o.OrderStatus == "Pending")
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    if (order.OrderDate.AddMinutes(1) <= now)
                    {
                        order.OrderStatus = "Cancelled";
                    }
                    else
                    {
                        // Süresini yenile (OrderDate güncelleniyor)
                        order.OrderDate = now;
                    }
                }

                // Ürün kontrolü
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

                // **Yeni Kontrol: Müşteri bir üründen en fazla 5 adet alabilir**
                var existingOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId && o.ProductID == productId && o.OrderStatus == "Pending")
                    .ToListAsync();

                int totalQuantity = existingOrders.Sum(o => o.Quantity) + quantity;
                if (totalQuantity > 5)
                {
                    return Json(new { success = false, message = "Bir üründen en fazla 5 adet alabilirsiniz." });
                }

                // Yeni siparişi ekle
                var newOrder = new Orders
                {
                    CustomerID = customerId.Value,
                    ProductID = productId,
                    Quantity = quantity,
                    OrderDate = now, // Siparişin oluşturulma zamanı
                    OrderStatus = "Pending",
                    TotalPrice = quantity * product.Price
                };

                _context.Orders.Add(newOrder);

                // Tüm değişiklikleri kaydet
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ürün sepete eklendi ve bekleyen siparişlerin süresi yenilendi!" });
            }
            catch (Exception ex)
            {
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

        [HttpPost]
        public async Task<IActionResult> ApproveAllCart()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

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

                // Siparişlerin toplam tutarını hesapla
                var totalAmount = pendingOrders.Sum(o => o.TotalPrice);

                // Müşteri bilgilerini kontrol et
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Müşteri bilgisi bulunamadı." });
                }

                if (customer.Budget < totalAmount)
                {
                    return Json(new { success = false, message = "Yetersiz bakiye!" });
                }

                // Siparişleri güncelle
                foreach (var order in pendingOrders)
                {
                    order.OrderStatus = "AwaitingApproval";
                    _context.Entry(order).State = EntityState.Modified;
                }

                // Log kayıtları oluştur
                var logs = pendingOrders.Select(order => new Logs
                {
                    CustomerID = customerId.Value,
                    OrderID = order.OrderID,
                    LogDate = DateTime.Now,
                    LogType = "StatusChange",
                    LogDetails = "Sipariş admin onayına gönderildi"
                }).ToList();

                await _context.Logs.AddRangeAsync(logs);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Sepetiniz başarıyla admin onayına gönderildi." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult ApproveOrder(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderID == orderId);
            if (order == null || order.OrderStatus != "Pending")
            {
                return Json(new { success = false, message = "Sipariş artık geçerli değil." });
            }

            order.OrderStatus = "AwaitingApproval"; // Durum güncelleniyor
            _context.SaveChanges();

            return Json(new { success = true, message = "Sipariş onaylandı!" });
        }

        [HttpPost]
        public async Task<IActionResult> CancelExpiredOrders()
        {
            try
            {
                var now = DateTime.Now;

                // Süresi dolmuş ve "Pending" durumundaki siparişleri al
                var expiredOrders = await _context.Orders
                    .Where(o => o.OrderStatus == "Pending" && o.OrderDate.AddMinutes(1) <= now)
                    .ToListAsync();

                if (!expiredOrders.Any())
                {
                    return Json(new { success = true, message = "Süresi dolmuş sipariş bulunamadı." });
                }

                // Süresi dolmuş siparişlerin durumunu "Cancelled" olarak güncelle
                foreach (var order in expiredOrders)
                {
                    order.OrderStatus = "Cancelled";
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"{expiredOrders.Count} adet süresi dolmuş sipariş iptal edildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderID == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş bulunamadı." });
                }

                order.OrderStatus = "Cancelled"; // OrderStatus değerini güncelle
                _context.Orders.Update(order); // Güncelleme işlemi
                _context.SaveChanges(); // Değişiklikleri veritabanına kaydet

                return Json(new { success = true, message = "Sipariş başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

    }
}
