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
            var awaitingApprovalOrders = _context.Orders
                .Include(o => o.Product)
                .Where(o => o.OrderStatus == "AwaitingApproval")
                .ToList();

            return View(awaitingApprovalOrders);
        }

        // Tek sipariş onaylama
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

                order.Product.Stock -= order.Quantity;
                order.OrderStatus = "Completed";
                _context.SaveChanges();

                return Json(new { success = true, message = "Sipariş onaylandı ve tamamlandı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu." });
            }
        }

        // Tüm siparişleri onaylama
        [HttpPost]
        public IActionResult ApproveAllOrders()
        {
            try
            {
                var awaitingApprovalOrders = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.OrderStatus == "AwaitingApproval")
                    .ToList();

                if (!awaitingApprovalOrders.Any())
                {
                    return Json(new { success = false, message = "Onaylanacak sipariş bulunamadı." });
                }

                foreach (var order in awaitingApprovalOrders)
                {
                    if (order.Product.Stock >= order.Quantity)
                    {
                        order.Product.Stock -= order.Quantity;
                        order.OrderStatus = "Completed";
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Sipariş {order.OrderID} için yetersiz stok." });
                    }
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Tüm siparişler başarıyla onaylandı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        // Tek sipariş silme
        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş bulunamadı." });
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Sipariş başarıyla silindi." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sipariş silinirken bir hata oluştu." });
            }
        }

        // Tüm siparişleri silme
        [HttpPost]
        public async Task<IActionResult> DeleteAllOrders()
        {
            try
            {
                var orders = await _context.Orders.ToListAsync();
                _context.Orders.RemoveRange(orders);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tüm siparişler başarıyla silindi." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Siparişler silinirken bir hata oluştu." });
            }
        }

        // Ürün ekleme sayfası
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        // Ürün ekleme işlemi
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
                    TempData["SuccessMessage"] = "Ürün başarıyla eklendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
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

        // Ürün düzenleme işlemi
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
                    TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
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
            // "Completed" siparişleri veritabanından çek
            var completedOrders = _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Customer)
                .Where(o => o.OrderStatus == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(completedOrders);
        }
    }
}