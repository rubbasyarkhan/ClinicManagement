using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicManagement.Controllers
{
    public class EventRegistrationsController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public EventRegistrationsController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // ✅ Get all event registrations (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var registrations = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .ToListAsync();

            return View(registrations);
        }

        // ✅ Get details of a specific registration (Admin or User who registered)
        // ✅ Show confirmation page for canceling registration (User can only cancel their own registration)
        [Authorize]
        public async Task<IActionResult> CancelRegistration(int? id)
        {
            if (id == null) return NotFound();

            var eventRegistration = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.RegistrationId == id);

            if (eventRegistration == null) return NotFound();

            // Ensure user can only cancel their own registration (unless Admin)
            if (!User.IsInRole("Admin") && eventRegistration.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return View(eventRegistration); // Show confirmation page
        }

        // ✅ Handle canceling event registration

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelRegistrationConfirmed(int id)
        {
            var eventRegistration = await _context.EventRegistrations.FindAsync(id);
            if (eventRegistration == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ensure only the user who registered or an Admin can cancel
            if (!User.IsInRole("Admin") && eventRegistration.UserId.ToString() != userId)
            {
                return Forbid();
            }

            _context.EventRegistrations.Remove(eventRegistration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your event registration has been successfully canceled.";

            return RedirectToAction("MyRegistrations");
        }



        // ✅ Show form for user to register for an event
        [Authorize]
        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName");
            return View();
        }

        // ✅ Handle user registration for an event
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("EventId")] EventRegistration eventRegistration)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); // Ensure user is logged in
            }

            eventRegistration.UserId = int.Parse(userId); // Assign logged-in user's ID
            eventRegistration.RegistrationDate = System.DateTime.Now;

            if (ModelState.IsValid)
            {
                // Check if user is already registered for this event
                bool alreadyRegistered = await _context.EventRegistrations
                    .AnyAsync(r => r.UserId == eventRegistration.UserId && r.EventId == eventRegistration.EventId);

                if (alreadyRegistered)
                {
                   
                    ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName", eventRegistration.EventId);
                    return View(eventRegistration);
                }

                _context.Add(eventRegistration);
                await _context.SaveChangesAsync();
                return RedirectToAction("MyRegistrations");
            }

            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName", eventRegistration.EventId);
            return View(eventRegistration);
        }
        [Authorize]
        public async Task<IActionResult> Register(int eventId)
        {
            var eventDetails = await _context.EducationEvents.FindAsync(eventId);

            if (eventDetails == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            // Check if user is already registered for this event
            bool alreadyRegistered = await _context.EventRegistrations
                .AnyAsync(r => r.UserId.ToString() == userId && r.EventId == eventId);

            if (alreadyRegistered)
            {
                
                return RedirectToAction("MyRegistrations"); // Redirect to the user's registered events
            }

            var viewModel = new EventRegistration
            {
                EventId = eventDetails.EventId,
                RegistrationDate = System.DateTime.Now
            };

            return View(viewModel); // Show confirmation page
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegister(int EventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to register for an event.";
                return RedirectToAction("Register", new { eventId = EventId });
            }

            int parsedUserId = int.Parse(userId);

            // Check if the user is already registered for this event
            bool alreadyRegistered = await _context.EventRegistrations
                .AnyAsync(r => r.UserId == parsedUserId && r.EventId == EventId);

            if (alreadyRegistered)
            {
                TempData["ErrorMessage"] = "You are already registered for this event.";
                return RedirectToAction("Register", new { eventId = EventId });
            }

            // Create a new registration entry
            var registration = new EventRegistration
            {
                EventId = EventId,
                UserId = parsedUserId,
                RegistrationDate = DateTime.Now
            };

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful!";
            return RedirectToAction("MyRegistrations");
        }



            // ✅ Show list of events the logged-in user has registered for
            [Authorize]
            public async Task<IActionResult> MyRegistrations()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var userRegistrations = await _context.EventRegistrations
                    .Include(e => e.Event)
                    .Where(e => e.UserId.ToString() == userId)
                    .ToListAsync();

                return View(userRegistrations);
            }

        // ✅ Show confirmation page for deletion (Only allow user to delete their own registration)
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var eventRegistration = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.RegistrationId == id);

            if (eventRegistration == null) return NotFound();

            // Ensure user can only delete their own registration
            if (!User.IsInRole("Admin") && eventRegistration.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return View(eventRegistration);
        }

        // ✅ Handle user deleting their own registration
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventRegistration = await _context.EventRegistrations.FindAsync(id);

            if (eventRegistration == null) return NotFound();

            // Ensure user can only delete their own registration
            if (!User.IsInRole("Admin") && eventRegistration.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            _context.EventRegistrations.Remove(eventRegistration);
            await _context.SaveChangesAsync();
            return RedirectToAction("MyRegistrations");
        }

        // ✅ Check if a registration exists
        private bool EventRegistrationExists(int id)
        {
            return _context.EventRegistrations.Any(e => e.RegistrationId == id);
        }
    }
}
