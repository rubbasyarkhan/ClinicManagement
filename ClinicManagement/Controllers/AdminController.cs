using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ClinicManagementDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ClinicManagementDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Admin Dashboard
        public IActionResult Index()
        {
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalTools = _context.Products.Count(p => p.Category != null && p.Category.CategoryName == "Medical Equipment");
            ViewBag.TotalMedicines = _context.Products.Count(p => p.Category == null || p.Category.CategoryName != "Medical Equipment");

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();

            ViewBag.TotalRevenue = _context.OrderDetails
                .Where(od => od.Order.OrderStatus == "Delivered")
                .Sum(od => (decimal?)od.Quantity * od.Price) ?? 0;

            ViewBag.OrdersPerStatus = _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            return View();
        }


        // Manage Products
        public IActionResult Products() => View(_context.Products.Include(p => p.Category).AsNoTracking().ToList());

        public IActionResult Medicines() => View(_context.Products.Include(p => p.Category).Where(p => p.Category.CategoryName != "Medical Equipment").AsNoTracking().ToList());

        public IActionResult Tools() => View(_context.Products.Include(p => p.Category).Where(p => p.Category.CategoryName == "Medical Equipment").AsNoTracking().ToList());

        [HttpGet]
        public IActionResult AddProduct()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

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
                if (ProductImage != null)
                    product.ImageUrl = SaveImage(ProductImage);

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

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

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
            if (existingProduct == null) return NotFound();

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Manufacturer = product.Manufacturer;
            existingProduct.CategoryId = product.CategoryId;

            if (ImageFile != null)
            {
                DeleteImage(existingProduct.ImageUrl);
                existingProduct.ImageUrl = SaveImage(ImageFile);
            }

            _context.Products.Update(existingProduct);
            _context.SaveChanges();
            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction(nameof(Products));
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Feedbacks) // Ensure navigation exists
                .ThenInclude(f => f.User)  // Include user who gave feedback
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        public IActionResult SubmitFeedback(int productId, string comment, int rating)
        {
            var userId = User.Identity.Name; // Assuming username is stored
            var user = _context.Users.FirstOrDefault(u => u.Username == userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found. Please log in." });
            }

            var feedback = new Feedback
            {
                ProductId = productId,
                UserId = user.UserId, // Assuming you have UserId
                Comment = comment,
                Rating = rating,
                CreatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                user = user.FullName,
                comment,
                rating,
                date = feedback.CreatedAt?.ToShortDateString() ?? "N/A"
            });

        }


        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Include(p => p.Category).AsNoTracking().FirstOrDefault(p => p.ProductId == id);
            return product == null ? NotFound() : View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            DeleteImage(product.ImageUrl);
            _context.Products.Remove(product);
            _context.SaveChanges();
            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Products));
        }

        // Helper Methods
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