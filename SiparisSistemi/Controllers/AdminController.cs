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
            var products = _context.Products.ToList();
            return View(products);
        }

        // Sipariş onaylama sayfası
        [HttpGet]
        public IActionResult OrdersApproval()
        {
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
                AddLog(null, null, "Error", $"Sipariş onaylama sayfası yüklenirken hata: {ex.Message}");
                return View(new List<Orders>());
            }
        }

        // Tek sipariş onaylama
        [HttpPost]
        public IActionResult ApproveOrder(int orderId)
        {
            try
            {
                var order = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Customer)
                    .FirstOrDefault(o => o.OrderID == orderId && o.OrderStatus == "AwaitingApproval");

                if (order == null)
                {
                    AddLog(null, orderId, "Error", $"Sipariş {orderId} bulunamadı veya zaten tamamlanmış.");
                    return Json(new { success = false, message = "Sipariş bulunamadı veya zaten tamamlanmış." });
                }

                if (order.Product.Stock < order.Quantity)
                {
                    AddLog(order.CustomerID, orderId, "Error", $"Sipariş {orderId}: Yetersiz stok.");
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                // Siparişi onayla
                order.Product.Stock -= order.Quantity;
                order.OrderStatus = "Completed";
                _context.SaveChanges();

                AddLog(order.CustomerID, orderId, "OrderApproved", $"Sipariş {order.OrderID} onaylandı. Ürün: {order.Product.ProductName}, Miktar: {order.Quantity}");

                return Json(new { success = true, message = "Sipariş onaylandı." });
            }
            catch (Exception ex)
            {
                AddLog(null, orderId, "Error", $"Sipariş {orderId} onaylanırken hata: {ex.Message}");
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
                AddLog(null, null, "Error", $"Loglar alınırken hata: {ex.Message}");
                return Json(new { success = false, message = "Loglar alınırken bir hata oluştu." });
            }
        }


        public IActionResult AllOrders()
        {
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

                // Öncelik Skorlarını Hesapla ve Siparişleri Sırala
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

                // Thread-safe stok erişimi için lock objesi
                object stockLock = new object();

                foreach (var item in sortedOrders)
                {
                    var order = item.Order;

                    lock (stockLock)
                    {
                        if (order.Product.Stock >= order.Quantity)
                        {
                            order.Product.Stock -= order.Quantity;
                            order.OrderStatus = "Completed";

                            // Başarılı sipariş log kaydı
                            _context.Logs.Add(new Logs
                            {
                                CustomerID = order.CustomerID,
                                OrderID = order.OrderID,
                                LogDate = DateTime.Now,
                                LogType = "Başarılı",
                                LogDetails = $"Sipariş {order.OrderID} tamamlandı. Ürün: {order.Product.ProductName}, Miktar: {order.Quantity}"
                            });
                        }
                        else
                        {
                            order.OrderStatus = "Cancelled";

                            // İptal edilen sipariş log kaydı
                            _context.Logs.Add(new Logs
                            {
                                CustomerID = order.CustomerID,
                                OrderID = order.OrderID,
                                LogDate = DateTime.Now,
                                LogType = "Hata",
                                LogDetails = $"Sipariş {order.OrderID} iptal edildi. Yetersiz stok."
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
                // Hata log kaydı
                _context.Logs.Add(new Logs
                {
                    CustomerID = 0, // Admin işlemi olduğu için CustomerID 0 olarak kaydediliyor
                    LogDate = DateTime.Now,
                    LogType = "Hata",
                    LogDetails = $"ApproveAllOrders işleminde hata: {ex.Message}"
                });
                _context.SaveChanges();

                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    AddLog(null, orderId, "Error", $"Sipariş {orderId} bulunamadı.");
                    return Json(new { success = false, message = "Sipariş bulunamadı." });
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                AddLog(order.CustomerID, orderId, "OrderDeleted", $"Sipariş {orderId} başarıyla silindi.");
                return Json(new { success = true, message = "Sipariş başarıyla silindi." });
            }
            catch (Exception ex)
            {
                AddLog(null, orderId, "Error", $"Sipariş {orderId} silinirken hata: {ex.Message}");
                return Json(new { success = false, message = "Sipariş silinirken bir hata oluştu." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAllOrders()
        {
            try
            {
                var orders = await _context.Orders.ToListAsync();
                if (!orders.Any())
                {
                    AddLog(null, null, "Error", "Silinecek sipariş bulunamadı.");
                    return Json(new { success = false, message = "Silinecek sipariş bulunamadı." });
                }

                _context.Orders.RemoveRange(orders);
                await _context.SaveChangesAsync();

                AddLog(null, null, "AllOrdersDeleted", "Tüm siparişler başarıyla silindi.");
                return Json(new { success = true, message = "Tüm siparişler başarıyla silindi." });
            }
            catch (Exception ex)
            {
                AddLog(null, null, "Error", $"Tüm siparişler silinirken hata: {ex.Message}");
                return Json(new { success = false, message = "Siparişler silinirken bir hata oluştu." });
            }
        }

        // Ürün ekleme sayfası
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
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

                    // Admin işlemi olduğu için CustomerID null gönderiliyor
                    AddLog(null, null, "ProductAdded", $"Ürün {product.ProductName} başarıyla eklendi.");
                    TempData["SuccessMessage"] = "Ürün başarıyla eklendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    AddLog(null, null, "Error", $"Ürün {product.ProductName} eklenirken hata: {ex.Message}");
                    ModelState.AddModelError("", "Ürün eklenirken bir hata oluştu: " + ex.Message);
                }
            }
            return View(product);
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
                        AddLog(null, null, "Error", $"Ürün {product.ProductID} bulunamadı.");
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

                    await _context.SaveChangesAsync();

                    // Admin işlemi olduğu için CustomerID null gönderiliyor
                    AddLog(null, null, "ProductEdited", $"Ürün {product.ProductName} başarıyla güncellendi.");
                    TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    AddLog(null, null, "Error", $"Ürün {product.ProductName} güncellenirken hata: {ex.Message}");
                    ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu: " + ex.Message);
                }
            }
            return View(product);
        }


        // Ürün silme işlemi
        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product != null)
                {
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
            // Tüm siparişleri getir
            var allOrders = _context.Orders
                .Include(o => o.Product) // Ürün bilgilerini dahil et
                .Include(o => o.Customer) // Müşteri bilgilerini dahil et
                .OrderByDescending(o => o.OrderDate) // Tarihe göre sırala
                .ToList();

            return View(allOrders);
        } // Log kaydetme helper metodu
        // Log kaydetme helper metodu
        private void AddLog(int? customerId, int? orderId, string logType, string logDetails)
        {
            var log = new Logs
            {
                CustomerID = customerId ?? 0,
                OrderID = orderId,
                LogDate = DateTime.Now,
                LogType = logType,
                LogDetails = logDetails
            };

            try
            {
                _context.Logs.Add(log);
                _context.SaveChanges();
                System.Diagnostics.Debug.WriteLine("Log başarıyla kaydedildi.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log kaydedilemedi: {ex.Message}");
            }
        }
    }
}