using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Collections.Generic;

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

            // 🔹 Optimized Total Revenue Calculation
            decimal totalRevenue = _context.OrderDetails
                .Where(od => od.Order.OrderStatus == "Delivered")
                .Sum(od => (decimal?)od.Quantity * od.Price) ?? 0;
            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.OrdersPerStatus = _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            return View();
        }

        // 🔹 Manage Products
        public IActionResult Products()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .ToList();
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

            try
            {
                if (ProductImage != null && ProductImage.Length > 0)
                {
                    product.ImageUrl = SaveImage(ProductImage);
                }

                _context.Products.Add(product);
                _context.SaveChanges();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                return View(product);
            }
        }

        // 🔹 Edit Product View
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
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

            try
            {
                var existingProduct = _context.Products.Find(product.ProductId);
                if (existingProduct == null)
                    return NotFound();

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Manufacturer = product.Manufacturer;
                existingProduct.CategoryId = product.CategoryId;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    DeleteImage(existingProduct.ImageUrl);
                    existingProduct.ImageUrl = SaveImage(ImageFile);
                }

                _context.Products.Update(existingProduct);
                _context.SaveChanges();
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                return View(product);
            }
        }

        // 🔹 Product Details Page
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // 🔹 Delete Product
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product == null)
                    return NotFound();

                DeleteImage(product.ImageUrl);
                _context.Products.Remove(product);
                _context.SaveChanges();

                TempData["Success"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                return RedirectToAction(nameof(Products));
            }
        }

        // 🔹 Helper Method: Check if Product Exists
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        // 🔹 Helper Method: Save Image
        private string SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            return "/images/" + uniqueFileName;
        }

        // 🔹 Helper Method: Delete Image
        private void DeleteImage(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
        }
    }
}
