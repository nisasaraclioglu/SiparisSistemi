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

        public IActionResult Dashboard()
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (adminId == null)
            {
                return RedirectToAction("CustomerLogin", "Login");
            }

            // Tüm ürünleri getir
            var products = _context.Products.ToList();
            return View(products);
        }

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
                        // Resim için benzersiz bir isim oluştur
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        
                        // Resmin kaydedileceği yol
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        
                        // Klasör yoksa oluştur
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        
                        // Resmin tam yolu
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        // Resmi kaydet
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        
                        // Veritabanında saklanacak yol
                        product.ImagePath = "/images/products/" + uniqueFileName;
                    }
                    else
                    {
                        // Varsayılan resim yolu
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
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = _context.Products.Find(product.ProductID);
                    if (existingProduct == null)
                    {
                        TempData["ErrorMessage"] = "Ürün bulunamadı.";
                        return View(product);
                    }

                    // Yeni resim yüklendiyse
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Eski resmi sil (varsayılan resim değilse)
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath) && 
                            !existingProduct.ImagePath.EndsWith("default.jpg") &&
                            System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/'))))
                        {
                            System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/')));
                        }

                        // Yeni resmi kaydet
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

                    // Diğer alanları güncelle
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.ProductType = product.ProductType;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction("Dashboard");
                }
                return View(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu: " + ex.Message);
                return View(product);
            }
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product != null)
                {
                    // Debug için yol bilgilerini yazdır
                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var imagePath = product.ImagePath?.TrimStart('/');
                    var fullPath = Path.Combine(webRootPath, imagePath ?? "");
                    
                    System.Diagnostics.Debug.WriteLine($"WebRoot Path: {webRootPath}");
                    System.Diagnostics.Debug.WriteLine($"Image Path: {imagePath}");
                    System.Diagnostics.Debug.WriteLine($"Full Path: {fullPath}");

                    // Ürün resmini sil (varsayılan resim değilse)
                    if (!string.IsNullOrEmpty(product.ImagePath) && 
                        !product.ImagePath.EndsWith("default.jpg") &&
                        System.IO.File.Exists(fullPath))
                    {
                        try
                        {
                            System.IO.File.Delete(fullPath);
                            System.Diagnostics.Debug.WriteLine("Resim başarıyla silindi.");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Resim silinirken hata: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Resim bulunamadı veya varsayılan resim.");
                    }

                    // Ürünü veritabanından sil
                    _context.Products.Remove(product);
                    _context.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Ürün ve ilgili resim başarıyla silindi.";
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silme işleminde hata: {ex.Message}");
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu: " + ex.Message });
            }
        }
    }
}
