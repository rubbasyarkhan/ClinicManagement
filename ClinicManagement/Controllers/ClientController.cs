using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models; // Adjust namespace if needed
using System.Linq;

namespace ClinicManagement.Controllers
{
    public class ClientController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public ClientController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Products()
        {
            var products = _context.Products
                .Include(p => p.Category) // Include category details if needed
                .AsNoTracking() // Optimize read-only queries
                .ToList();
            return View(products);
        }

        public IActionResult ProductDetails(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Medicines Page
        public IActionResult Medicines()
        {
            var medicines = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.Category != null && p.Category.CategoryName != "Medical Equipment") // Null check
                .ToList();

            return View(medicines);
        }

        // Medical Equipment Page
        public IActionResult MedicalEquipment()
        {
            var equipment = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.Category != null && p.Category.CategoryName == "Medical Equipment") // Null check
                .ToList();

            return View(equipment);
        }
    }
}
