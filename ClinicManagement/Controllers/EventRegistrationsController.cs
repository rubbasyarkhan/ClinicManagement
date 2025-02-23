using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    public class EventRegistrationsController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public EventRegistrationsController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // ✅ Get all event registrations
        public async Task<IActionResult> Index()
        {
            var registrations = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .ToListAsync();

            return View(registrations);
        }

        // ✅ Get details of a specific registration
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var eventRegistration = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.RegistrationId == id);

            if (eventRegistration == null) return NotFound();

            return View(eventRegistration);
        }

        // ✅ Show form to create a new registration
        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName");
            return View();
        }

        // ✅ Handle registration creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,UserId,RegistrationDate")] EventRegistration eventRegistration)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventRegistration);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName", eventRegistration.EventId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", eventRegistration.UserId);
            return View(eventRegistration);
        }

        // ✅ Show form to edit a registration
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var eventRegistration = await _context.EventRegistrations.FindAsync(id);
            if (eventRegistration == null) return NotFound();

            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName", eventRegistration.EventId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", eventRegistration.UserId);
            return View(eventRegistration);
        }

        // ✅ Handle registration updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RegistrationId,EventId,UserId,RegistrationDate")] EventRegistration eventRegistration)
        {
            if (id != eventRegistration.RegistrationId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventRegistration);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventRegistrationExists(eventRegistration.RegistrationId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["EventId"] = new SelectList(_context.EducationEvents, "EventId", "EventName", eventRegistration.EventId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", eventRegistration.UserId);
            return View(eventRegistration);
        }

        // ✅ Show confirmation page for deletion
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var eventRegistration = await _context.EventRegistrations
                .Include(e => e.Event)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.RegistrationId == id);

            if (eventRegistration == null) return NotFound();

            return View(eventRegistration);
        }

        // ✅ Handle registration deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventRegistration = await _context.EventRegistrations.FindAsync(id);
            if (eventRegistration != null)
            {
                _context.EventRegistrations.Remove(eventRegistration);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ✅ Check if a registration exists
        private bool EventRegistrationExists(int id)
        {
            return _context.EventRegistrations.Any(e => e.RegistrationId == id);
        }
    }
}
