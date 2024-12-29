using Microsoft.AspNetCore.Mvc;
using SiparisSistemi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace SiparisSistemi.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard sayfası
        public IActionResult Dashboard()
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (adminId == null)
            {
                return RedirectToAction("CustomerLogin", "Login");
            }

            var lockedId = HttpContext.Session.GetInt32("LockedProductID");
            if (lockedId.HasValue)
            {
                var p = _context.Products.Find(lockedId.Value);
                if (p != null && p.IsLocked)
                {
                    p.IsLocked = false;
                    _context.SaveChanges();
                }

                // Session’dan sil
                HttpContext.Session.Remove("LockedProductID");
            }

            var products = _context.Products.ToList();

            // Grafik için stok verilerini hesapla
            var productStockData = products.Select(p => new
            {
                ProductName = p.ProductName,
                Stock = p.Stock,
                Percentage = (products.Sum(prod => prod.Stock) > 0)
                    ? (decimal)p.Stock / products.Sum(prod => prod.Stock) * 100
                    : 0
            }).ToList();

            // Stok verilerini ViewData ile frontend'e gönder
            ViewData["ProductStockData"] = Newtonsoft.Json.JsonConvert.SerializeObject(productStockData);

            return View(products);
        }


        // Sipariş onaylama sayfası
        [HttpGet]
        public IActionResult OrdersApproval()
        {
            var lockedId = HttpContext.Session.GetInt32("LockedProductID");
            if (lockedId.HasValue)
            {
                var p = _context.Products.Find(lockedId.Value);
                if (p != null && p.IsLocked)
                {
                    p.IsLocked = false;
                    _context.SaveChanges();
                }

                // Session’dan sil
                HttpContext.Session.Remove("LockedProductID");
            }

            try
            {
                var awaitingApprovalOrders = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.OrderStatus == "AwaitingApproval")
                    .ToList();

                return View(awaitingApprovalOrders);
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Hata", $"Sipariş onaylama sayfası yüklenirken hata: {ex.Message}");
                return View(new List<Orders>());
            }
        }

        // Tek sipariş onaylama
        [HttpPost]
        public IActionResult ApproveOrder(int orderId)
        {
            try
            {
        // Siparişi veritabanından çek
        var order = _context.Orders
            .Include(o => o.Product)
            .Include(o => o.Customer)
            .FirstOrDefault(o => o.OrderID == orderId && o.OrderStatus == "AwaitingApproval");

        if (order == null)
        {
            return Json(new { success = false, message = "Sipariş bulunamadı veya zaten tamamlanmış." });
        }

        // Yetersiz stok kontrolü
        if (order.Product.Stock < order.Quantity)
        {
            AddLog(order.CustomerID, orderId, "Hata", "Yetersiz stok.");
            return Json(new { success = false, message = "Yetersiz stok." });
        }

        // Yetersiz bütçe kontrolü
        if (order.Customer.Budget < order.TotalPrice)
        {
            AddLog(order.CustomerID, orderId, "Hata", "Yetersiz bütçe.");
            return Json(new { success = false, message = "Yetersiz bütçe." });
        }

        // Stok ve bütçe güncellemeleri
        _context.Attach(order.Customer); // EF Core'a müşteri takibini belirt
        order.Product.Stock -= order.Quantity;
        order.Customer.Budget -= order.TotalPrice;
        order.Customer.TotalSpent += order.TotalPrice;

                // Müşteri tipini kontrol et ve güncelle
        if (order.Customer.TotalSpent >= 2000 && order.Customer.CustomerType == "Standard")
            {
            order.Customer.CustomerType = "Premium";

            // Premium'a yükselme log kaydı
            _context.Logs.Add(new Logs
                    {
                        CustomerID = order.CustomerID,
                        OrderID = order.OrderID,
                        LogDate = DateTime.Now,
                        LogType = "Bilgilendirme",
                        LogDetails = $"Müşteri {order.Customer.CustomerName} toplam {order.Customer.TotalSpent:C2} harcama ile Premium üyeliğe yükseltildi."
                    });
                }

        // Sipariş durumunu güncelle
        order.OrderStatus = "Completed";

        // Değişiklikleri kaydet
        _context.SaveChanges();

        AddLog(order.CustomerID, orderId, "Bilgilendirme", $"Sipariş {order.OrderID} başarıyla onaylandı.");
        return Json(new { success = true, message = "Sipariş başarıyla onaylandı." });
        }
        catch (Exception ex)
            {
            AddLog(null, orderId, "Hata", $"Sipariş onaylanırken hata: {ex.Message}");
            return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        // Logları frontend'e gönderen endpoint
        [HttpGet]
        public IActionResult GetLogs()
        {
            try
            {
                var logs = _context.Logs
                    .OrderByDescending(l => l.LogDate)
                    .Take(100)
                    .ToList();

                return Json(logs);
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Hata", $"Loglar alınırken hata: {ex.Message}");
                return Json(new { success = false, message = "Loglar alınırken bir hata oluştu." });
            }
        }

        public IActionResult AllOrders()
        {
            var lockedId = HttpContext.Session.GetInt32("LockedProductID");
            if (lockedId.HasValue)
            {
                var p = _context.Products.Find(lockedId.Value);
                if (p != null && p.IsLocked)
                {
                    p.IsLocked = false;
                    _context.SaveChanges();
                }

                // Session’dan sil
                HttpContext.Session.Remove("LockedProductID");
            }
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .ToList(); // Tüm siparişler

            return View(orders);
        }

        // Tüm siparişleri onaylama
        [HttpPost]
        public IActionResult ApproveAllOrders()
        {
            try
            {
                var awaitingApprovalOrders = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Customer)
                    .Where(o => o.OrderStatus == "AwaitingApproval")
                    .ToList();

                if (!awaitingApprovalOrders.Any())
                {
                    return Json(new { success = false, message = "Onaylanacak sipariş bulunamadı." });
                }

                const double BeklemeSureAgi = 0.5;

                var sortedOrders = awaitingApprovalOrders
                    .Select(order =>
                    {
                        var logDate = _context.Logs
                            .Where(log => log.OrderID == order.OrderID)
                            .OrderBy(log => log.LogDate)
                            .Select(log => log.LogDate)
                            .FirstOrDefault();

                        var orderDate = logDate != default ? logDate : order.OrderDate;

                        return new
                        {
                            Order = order,
                            PriorityScore = (order.Customer.CustomerType == "Premium" ? 15 : 10) +
                                            (DateTime.Now - orderDate).TotalSeconds * BeklemeSureAgi
                        };
                    })
                    .OrderByDescending(x => x.PriorityScore)
                    .ToList();

                object stockLock = new object();

                foreach (var item in sortedOrders)
                {
                    var order = item.Order;

                    lock (stockLock)
                    {
                        if (order.Product.Stock >= order.Quantity && order.Customer.Budget >= order.TotalPrice)
                        {
                            // Stok ve müşteri bütçesini güncelle
                            order.Product.Stock -= order.Quantity;
                            order.Customer.Budget -= order.TotalPrice;
                            order.Customer.TotalSpent += order.TotalPrice;
                            order.OrderStatus = "Completed";

                            // Müşteri tipini kontrol et ve güncelle
                            if (order.Customer.TotalSpent >= 2000 && order.Customer.CustomerType == "Standard")
                            {
                                order.Customer.CustomerType = "Premium";

                                // Premium'a yükselme log kaydı
                                _context.Logs.Add(new Logs
                                {
                                    CustomerID = order.CustomerID,
                                    OrderID = order.OrderID,
                                    LogDate = DateTime.Now,
                                    LogType = "Bilgilendirme",
                                    LogDetails = $"Müşteri {order.Customer.CustomerName} toplam {order.Customer.TotalSpent:C2} harcama ile Premium üyeliğe yükseltildi."
                                });
                            }

                            // Başarılı sipariş log kaydı
                            _context.Logs.Add(new Logs
                            {
                                CustomerID = order.CustomerID,
                                OrderID = order.OrderID,
                                LogDate = DateTime.Now,
                                LogType = "Bilgilendirme",
                                LogDetails = $"Sipariş {order.OrderID} tamamlandı. Ürün: {order.Product.ProductName}, Miktar: {order.Quantity}"
                            });
                        }
                        else
                        {
                            order.OrderStatus = "Cancelled";

                            // Hata nedeni belirleme
                            var errorReason = order.Product.Stock < order.Quantity ? "Yetersiz stok" : "Yetersiz bütçe";

                            // İptal edilen sipariş log kaydı
                            _context.Logs.Add(new Logs
                            {
                                CustomerID = order.CustomerID,
                                OrderID = order.OrderID,
                                LogDate = DateTime.Now,
                                LogType = "Hata",
                                LogDetails = $"Sipariş {order.OrderID} iptal edildi. {errorReason}."
                            });
                        }
                    }
                }

                // Değişiklikleri kaydet
                _context.SaveChanges();

                return Json(new { success = true, message = "Siparişler önceliğe göre işleme alındı!" });
            }
            catch (Exception ex)
            {
                _context.Logs.Add(new Logs
                {
                    CustomerID = 0,
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"ApproveAllOrders işleminde hata: {ex.Message}"
                });
                _context.SaveChanges();

                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult RejectAllOrders()
        {
            try
            {
                var awaitingApprovalOrders = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Customer)
                    .Where(o => o.OrderStatus == "AwaitingApproval")
                    .ToList();

                if (!awaitingApprovalOrders.Any())
                {
                    return Json(new { success = false, message = "Reddedilecek sipariş bulunamadı." });
                }

                foreach (var order in awaitingApprovalOrders)
                {
                    order.OrderStatus = "Cancelled";

                    // Log kaydı ekle
                    _context.Logs.Add(new Logs
                    {
                        CustomerID = order.CustomerID,
                        OrderID = order.OrderID,
                        LogDate = DateTime.Now,
                        LogType = "Bilgilendirme",
                        LogDetails = $"Sipariş {order.OrderID} reddedildi. Ürün: {order.Product.ProductName}, Müşteri: {order.CustomerID}"
                    });
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Tüm siparişler başarıyla reddedildi." });
            }
            catch (Exception ex)
            {
                // Hata logu
                _context.Logs.Add(new Logs
                {
                    CustomerID = 0,
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"Toplu sipariş reddetme işleminde hata: {ex.Message}"
                });
                _context.SaveChanges();

                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        // Ürün ekleme sayfası
        [HttpGet]
        public IActionResult AddProduct()
        {
            var lockedId = HttpContext.Session.GetInt32("LockedProductID");
            if (lockedId.HasValue)
            {
                var p = _context.Products.Find(lockedId.Value);
                if (p != null && p.IsLocked)
                {
                    p.IsLocked = false;
                    _context.SaveChanges();
                }

                // Session’dan sil
                HttpContext.Session.Remove("LockedProductID");
            }
            return View();
        }

        // Ürün düzenleme sayfası
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            product.IsLocked = true;
            _context.SaveChanges();

            HttpContext.Session.SetInt32("LockedProductID", id);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Products product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        product.ImagePath = "/images/products/" + uniqueFileName;
                    }
                    else
                    {
                        product.ImagePath = "/images/products/default.jpg";
                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Log kaydı
                    var log = new Logs
                    {
                        CustomerID = null,
                        OrderID = null,
                        LogDate = DateTime.Now,
                        LogType = "Bilgilendirme",
                        LogDetails = $"Ürün eklendi - ID: {product.ProductID}, " +
                                   $"Adı: {product.ProductName}"
                    };

                    _context.Logs.Add(log);
                    await _context.SaveChangesAsync(); // Log kaydını kaydet

                    TempData["SuccessMessage"] = "Ürün başarıyla eklendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    // Hata durumunda log kaydı
                    var errorLog = new Logs
                    {
                        CustomerID = null,
                        OrderID = null,
                        LogDate = DateTime.Now,
                        LogType = "Hata",
                        LogDetails = $"Ürün ekleme hatası - " +
                                   $"Ürün: {product.ProductName}"
                    };

                    _context.Logs.Add(errorLog);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", "Ürün eklenirken bir hata oluştu: " + ex.Message);
                }
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Products product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = _context.Products.Find(product.ProductID);
                    if (existingProduct == null)
                    {
                        // Log kaydı
                        var errorlog = new Logs
                        {
                            CustomerID = null,
                            OrderID = null,
                            LogDate = DateTime.Now,
                            LogType = "Hata",
                            LogDetails = $"Ürün { product.ProductID } bulunamadı."
                        };

                        _context.Logs.Add(errorlog);
                        await _context.SaveChangesAsync(); // Log kaydını kaydet
                        
                        TempData["ErrorMessage"] = "Ürün bulunamadı.";
                        return View(product);
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath) &&
                            !existingProduct.ImagePath.EndsWith("default.jpg") &&
                            System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/'))))
                        {
                            System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/')));
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        existingProduct.ImagePath = "/images/products/" + uniqueFileName;
                    }

                    existingProduct.ProductName = product.ProductName;
                    existingProduct.ProductType = product.ProductType;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;

                    existingProduct.IsLocked = false;
                    await _context.SaveChangesAsync();

                    // Log kaydı
                    var log = new Logs
                    {
                        CustomerID = null,
                        OrderID = null,
                        LogDate = DateTime.Now,
                        LogType = "Bilgilendirme",
                        LogDetails = $"Ürün {product.ProductID} güncellendi."
                    };

                    _context.Logs.Add(log);
                    await _context.SaveChangesAsync(); // Log kaydını kaydet

                    TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    AddLog(null, null, "Hata", $"Ürün {product.ProductName} güncellenirken hata: {ex.Message}");
                    ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu: " + ex.Message);
                }
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product != null)
                {
                    // Orders tablosunda bu productID’yi kullanan tüm siparişler
                    var ordersToRemove = _context.Orders
                        .Where(o => o.ProductID == product.ProductID)
                        .ToList();

                    // Logs tablosunda bu siparişleri kullanan tüm loglar
                    var logIDs = ordersToRemove.Select(x => x.OrderID).ToList();
                    var logsToRemove = _context.Logs
                        .Where(l => l.OrderID.HasValue && logIDs.Contains(l.OrderID.Value))
                        .ToList();

                    // 1) Logs tablosundaki kayıtları sil
                    _context.Logs.RemoveRange(logsToRemove);

                    // 2) Orders tablosundaki kayıtları sil
                    _context.Orders.RemoveRange(ordersToRemove);

                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var imagePath = product.ImagePath?.TrimStart('/');
                    var fullPath = Path.Combine(webRootPath, imagePath ?? "");

                    if (!string.IsNullOrEmpty(product.ImagePath) &&
                        !product.ImagePath.EndsWith("default.jpg") &&
                        System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    _context.Products.Remove(product);
                    _context.SaveChanges();

                    // Log kaydı
                    var log = new Logs
                    {
                        CustomerID = null,
                        OrderID = null,
                        LogDate = DateTime.Now,
                        LogType = "Bilgilendirme",
                        LogDetails = $"Ürün {product.ProductID} silindi."
                    };

                    _context.Logs.Add(log);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Ürün başarıyla silindi.";
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ApprovedOrders()
        {
            var lockedId = HttpContext.Session.GetInt32("LockedProductID");
            if (lockedId.HasValue)
            {
                var p = _context.Products.Find(lockedId.Value);
                if (p != null && p.IsLocked)
                {
                    p.IsLocked = false;
                    _context.SaveChanges();
                }

                // Session’dan sil
                HttpContext.Session.Remove("LockedProductID");
            }
            // Tüm siparişleri getir
            var allOrders = _context.Orders
                .Include(o => o.Product) // Ürün bilgilerini dahil et
                .Include(o => o.Customer) // Müşteri bilgilerini dahil et
                .OrderByDescending(o => o.OrderDate) // Tarihe göre sırala
                .ToList();

            return View(allOrders);
        }

        [HttpPost]
        public IActionResult DeleteOrder(int orderId)
        {
            try
            {
                // Siparişi veritabanından bul
                var order = _context.Orders
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş bulunamadı." });
                }

                // Siparişin durumunu 'Cancelled' olarak güncelle
                order.OrderStatus = "Cancelled";

                // Değişiklikleri kaydet
                _context.SaveChanges();

                // Log ekle: İptal edilen sipariş
                AddLog(order.CustomerID, orderId, "Bilgilendirme", $"Sipariş {orderId} iptal edildi.");
                return Json(new { success = true, message = "Sipariş başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                // Hata mesajını logla
                AddLog(null, orderId, "Hata", $"Sipariş iptal edilirken hata oluştu: {ex.Message}");
                return Json(new { success = false, message = $"Sipariş iptal edilirken hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult FetchLogs()
        {
            var logs = _context.Logs
                .OrderByDescending(l => l.LogDate)
                .Take(20) // Son 20 log
                .Select(l => new
                {
                    l.LogType,
                    l.LogDetails,
                    LogDate = l.LogDate.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToList();

            return Json(logs);
        }

        private void AddLog(int? customerId, int? orderId, string logType, string logDetails)
        {
            try
            {
                var log = new Logs
                {
                    CustomerID = customerId ?? 0,
                    OrderID = orderId ?? 0,
                    LogDate = DateTime.Now,
                    LogType = logType,
                    LogDetails = logDetails
                };

                _context.Logs.Add(log);
                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"Log kaydedildi - Tür: {logType}, Detay: {logDetails}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log kaydedilemedi: {ex.Message}");

                // Hata durumunda ikinci bir deneme
                try
                {
                    _context.Logs.Add(new Logs
                    {
                        CustomerID = 0,
                        OrderID = 0,
                        LogDate = DateTime.Now,
                        LogType = "Error",
                        LogDetails = $"Log kayıt hatası: {ex.Message}"
                    });
                    _context.SaveChanges();
                }
                catch { /* En kötü durumda sessizce devam et */ }
            }
        }

        [HttpGet]
        public JsonResult GetAwaitingApprovalOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Customer)
                    .Where(o => o.OrderStatus == "AwaitingApproval")
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        orderID = o.OrderID,
                        productName = o.Product != null ? o.Product.ProductName : "Bilinmiyor",
                        customerID = o.CustomerID,
                        customerType = o.Customer.CustomerType,
                        quantity = o.Quantity,
                        totalPrice = o.TotalPrice,
                        orderDate = o.OrderDate,
                        status = o.OrderStatus
                    })
                    .ToList();

                return Json(new { success = true, orders });
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Hata", $"Bekleyen siparişler alınırken hata: {ex.Message}");
                return Json(new { success = false, message = "Siparişler alınırken bir hata oluştu." });
            }
        }
        [HttpGet]
        public JsonResult GetProductStockData()
        {
            var products = _context.Products.ToList();
            var totalStock = products.Sum(p => p.Stock);

            var productData = products.Select(p => new
            {
                ProductName = p.ProductName,
                Stock = p.Stock,
                Percentage = (double)p.Stock / totalStock * 100
            });

            return Json(productData);
        }


        [HttpGet]
        public async Task<IActionResult> GetNewOrders(int lastOrderId)
        {
            try
            {
                var newOrders = await _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Customer)
                    .Where(o => o.OrderID > lastOrderId && o.OrderStatus == "AwaitingApproval")
                    .OrderByDescending(o => o.OrderID)
                    .Select(o => new
                    {
                        orderID = o.OrderID,
                        productName = o.Product != null ? o.Product.ProductName : "Bilinmiyor",
                        customerID = o.CustomerID,
                        customerType = o.Customer.CustomerType,
                        quantity = o.Quantity,
                        totalPrice = o.TotalPrice,
                        orderDate = o.OrderDate,
                        status = o.OrderStatus
                    })
                    .ToListAsync();

                return Json(new { success = true, orders = newOrders });
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Hata", $"Yeni siparişler alınırken hata: {ex.Message}");
                return Json(new { success = false, message = "Siparişler alınırken bir hata oluştu." });
            }
        }
    }
}