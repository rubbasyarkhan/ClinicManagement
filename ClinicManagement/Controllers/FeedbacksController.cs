using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    public class FeedbacksController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public FeedbacksController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // GET: Feedbacks
        public async Task<IActionResult> Index()
        {
            var feedbacks = _context.Feedbacks.Include(f => f.Product).Include(f => f.User);
            return View(await feedbacks.ToListAsync());
        }

        // GET: Feedbacks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _context.Feedbacks
                .Include(f => f.Product)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);

            if (feedback == null) return NotFound();

            return View(feedback);
        }

        // GET: Feedbacks/Create
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name"); // Show product name instead of ID
            return View();
        }

        // POST: Feedbacks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Comment,Rating")] Feedback feedback)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || !int.TryParse(userId, out int parsedUserId))
            {
                ModelState.AddModelError("", "Invalid user ID. Please log in.");
                return View(feedback);
            }

            if (ModelState.IsValid)
            {
                feedback.UserId = parsedUserId;
                feedback.CreatedAt = DateTime.Now;

                _context.Add(feedback);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", feedback.ProductId);
            return View(feedback);
        }

        // GET: Feedbacks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return NotFound();

            // Only allow the feedback owner or admin to edit
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || (!User.IsInRole("Admin") && feedback.UserId.ToString() != userId))
            {
                return Forbid(); // Prevent unauthorized access
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", feedback.ProductId);
            return View(feedback);
        }

        // POST: Feedbacks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FeedbackId,ProductId,Comment,Rating,CreatedAt")] Feedback feedback)
        {
            if (id != feedback.FeedbackId) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || (!User.IsInRole("Admin") && feedback.UserId.ToString() != userId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(feedback);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeedbackExists(feedback.FeedbackId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", feedback.ProductId);
            return View(feedback);
        }

        // GET: Feedbacks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _context.Feedbacks
                .Include(f => f.Product)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);

            if (feedback == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || (!User.IsInRole("Admin") && feedback.UserId.ToString() != userId))
            {
                return Forbid();
            }

            return View(feedback);
        }

        // POST: Feedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null || (!User.IsInRole("Admin") && feedback.UserId.ToString() != userId))
                {
                    return Forbid();
                }

                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.FeedbackId == id);
        }
    }
}
