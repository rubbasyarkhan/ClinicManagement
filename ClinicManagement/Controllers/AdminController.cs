using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace ClinicManagement.Controllers
{
    public class AdminController : Controller
    {
        private readonly ClinicManagementDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ClinicManagementDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 🔹 Admin Dashboard
        public IActionResult Index()
        {
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            return View();
        }

        // 🔹 Manage Products
        public IActionResult Products()
        {
            var products = _context.Products.Include(p => p.Category).ToList(); // ✅ Include Category
            return View(products);
        }

        // 🔹 Add Product View
        [HttpGet]
        public IActionResult AddProduct()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        // 🔹 Add Product Logic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(Product product, IFormFile ProductImage)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return View(product);
            }

            if (ProductImage != null && ProductImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProductImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ProductImage.CopyTo(stream);
                }

                product.ImageUrl = "/images/" + uniqueFileName;
            }

            _context.Products.Add(product);
            _context.SaveChanges();
            return RedirectToAction("Products");
        }

        // 🔹 Edit Product View
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId); // ✅ Pre-select category
            return View(product);
        }

        // 🔹 Edit Product Logic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
                return View(product);
            }

            var existingProduct = _context.Products.Find(product.ProductId);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Update product properties
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Manufacturer = product.Manufacturer;
            existingProduct.CategoryId = product.CategoryId; // ✅ Ensure category is updated

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                existingProduct.ImageUrl = "/images/" + uniqueFileName;
            }

            _context.Products.Update(existingProduct);
            _context.SaveChanges();
            return RedirectToAction("Products"); // ✅ Redirect to Products List
        }

        // 🔹 Product Details Page
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category) // ✅ Include category details
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // 🔹 Delete Product
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.Category) // Include category details
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        // 🔹 Helper Method: Check if Product Exists
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
