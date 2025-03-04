using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models; // Adjust namespace if needed
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            var product = _context.Products
         .Include(p => p.Category)
         .AsNoTracking()
         .Take(6) // Fetch only 6 products
         .ToList();

            return View(product);
        }
        public IActionResult About()
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

        [Route("product/{id}")] // Optional: Pretty URL like /product/5
                                // Product Details (Used for both Medicines & Medical Equipment)
        public IActionResult ProductDetails(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Feedbacks)
                    .ThenInclude(f => f.User) // Include User details
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }



        [HttpPost]
        [Route("Client/SubmitFeedback")]
        [Authorize]
        public IActionResult SubmitFeedback(int ProductId, int Rating, string Comment)
        {
            Console.WriteLine($"Received Feedback: ProductId={ProductId}, Rating={Rating}, Comment={Comment}");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                Console.WriteLine("User not authenticated.");
                return Unauthorized();
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                Console.WriteLine("Invalid User ID.");
                return BadRequest("Invalid user ID.");
            }

            var feedback = new Feedback
            {
                ProductId = ProductId,
                UserId = userId,
                Rating = Rating,
                Comment = Comment,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();
                Console.WriteLine("Feedback saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving feedback: {ex.Message}");
                return BadRequest("Error saving feedback.");
            }

            return RedirectToAction("ProductDetails", "Client", new { id = ProductId });
        }


        [HttpGet]
        public IActionResult Test()
        {
            return Ok("Test endpoint is working!");
        }



        // Medicines Page
        public async Task<IActionResult> Medicines()
        {
            var medicines = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.Category != null && p.Category.CategoryName != "Medical Equipment")
                .ToListAsync();

            return View(medicines);
        }

        // Medical Equipment Page
        public async Task<IActionResult> MedicalEquipment()
        {
            var equipment = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.Category != null && p.Category.CategoryName == "Medical Equipment")
                .ToListAsync();

            return View(equipment);
        }
    }
}
