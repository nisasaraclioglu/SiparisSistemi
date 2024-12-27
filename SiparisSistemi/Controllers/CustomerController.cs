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
                AddLog(null, null, "Error", "Hesap bilgilerine erişim için oturum açılmadı.");
                return RedirectToAction("CustomerLogin", "Login");
            }

            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);

            if (customer == null)
            {
                AddLog(customerId, null, "Error", "Hesap bilgileri bulunamadı.");
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
        private void AddLog(int? customerId, int? orderId, string logType, string logDetails)
        {
            var log = new Logs
            {
                CustomerID = customerId ?? 0, // Eğer null ise 0 atanabilir.
                OrderID = orderId,
                LogDate = DateTime.Now,
                LogType = logType,
                LogDetails = logDetails
            };

            try
            {
                _context.Logs.Add(log);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Hata loglama
                System.Diagnostics.Debug.WriteLine($"Log kaydedilemedi: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddToCart(int productId, int quantity)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    AddLog(null, null, "Error", "Sepete ürün eklemek için giriş yapılmadı.");
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
                        order.OrderDate = now; // Süresini yenile
                    }
                }

                // Ürün kontrolü
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    AddLog(customerId, null, "Error", $"Ürün bulunamadı: {productId}");
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                // Stok kontrolü
                if (product.Stock < quantity)
                {
                    AddLog(customerId, null, "Error", $"Yetersiz stok: {product.ProductName}");
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                // Müşteri bir üründen en fazla 5 adet alabilir kontrolü
                var existingOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId && o.ProductID == productId && o.OrderStatus == "Pending")
                    .ToListAsync();

                int totalQuantity = existingOrders.Sum(o => o.Quantity) + quantity;
                if (totalQuantity > 5)
                {
                    AddLog(customerId, null, "Error", $"Maksimum sipariş limiti aşıldı: {product.ProductName}");
                    return Json(new { success = false, message = "Bir üründen en fazla 5 adet alabilirsiniz." });
                }

                // Yeni sipariş oluştur
                var newOrder = new Orders
                {
                    CustomerID = customerId.Value,
                    ProductID = productId,
                    Quantity = quantity,
                    OrderDate = now,
                    OrderStatus = "Pending",
                    TotalPrice = quantity * product.Price
                };

                _context.Orders.Add(newOrder);

                // Değişiklikleri kaydet
                await _context.SaveChangesAsync();
                AddLog(customerId, newOrder.OrderID, "AddToCart", $"Ürün sepete eklendi: {product.ProductName}, Miktar: {quantity}");

                return Json(new { success = true, message = "Ürün sepete eklendi ve bekleyen siparişlerin süresi yenilendi!" });
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Error", $"Sepete ürün eklenirken hata: {ex.Message}");
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult Orders()
        {
            // Session'dan CustomerID'yi al
            var customerId = HttpContext.Session.GetInt32("CustomerID");

            if (customerId == null)
            {
                AddLog(null, null, "Error", "Siparişlere erişim için giriş yapılmadı.");
                return RedirectToAction("CustomerLogin", "Login");
            }

            // Siparişleri veritabanından çek
            var orders = _context.Orders
                .Where(o => o.CustomerID == customerId)
                .Include(o => o.Product) // Ürün bilgilerini dahil et
                .ToList();

            if (orders == null || !orders.Any())
            {
                AddLog(customerId, null, "Info", "Müşteri için sipariş bulunamadı.");
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

                var pendingOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId && o.OrderStatus == "Pending")
                    .ToListAsync();

                if (!pendingOrders.Any())
                {
                    return Json(new { success = false, message = "Onaylanacak sipariş bulunamadı." });
                }

                var totalAmount = pendingOrders.Sum(o => o.TotalPrice);

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Müşteri bilgisi bulunamadı." });
                }

                if (customer.Budget < totalAmount)
                {
                    return Json(new { success = false, message = "Yetersiz bakiye!" });
                }

                foreach (var order in pendingOrders)
                {
                    order.OrderStatus = "AwaitingApproval";
                    _context.Entry(order).State = EntityState.Modified;
                }

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
                    AddLog(null, orderId, "Error", "İptal edilecek sipariş bulunamadı.");
                    return Json(new { success = false, message = "Sipariş bulunamadı." });
                }

                order.OrderStatus = "Cancelled"; // OrderStatus değerini güncelle
                _context.Orders.Update(order); // Güncelleme işlemi
                _context.SaveChanges(); // Değişiklikleri veritabanına kaydet

                AddLog(order.CustomerID, orderId, "CancelOrder", $"Sipariş iptal edildi: {orderId}");
                return Json(new { success = true, message = "Sipariş başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                AddLog(null, orderId, "Error", $"Sipariş iptali sırasında hata: {ex.Message}");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

    }
}
