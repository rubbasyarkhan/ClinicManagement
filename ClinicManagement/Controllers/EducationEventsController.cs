using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EducationEventsController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public EducationEventsController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // GET: EducationEvents
        public async Task<IActionResult> Index()
        {
            return View(await _context.EducationEvents.ToListAsync());
        }

        // GET: EducationEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var educationEvent = await _context.EducationEvents
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (educationEvent == null)
            {
                return NotFound();
            }

            return View(educationEvent);
        }

        // GET: EducationEvents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EducationEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,EventName,EventDate,EventTime,Speaker,Description")] EducationEvent educationEvent)
        {
            if (ModelState.IsValid)
            {
                _context.Add(educationEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(educationEvent);
        }

        // GET: EducationEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var educationEvent = await _context.EducationEvents.FindAsync(id);
            if (educationEvent == null)
            {
                return NotFound();
            }
            return View(educationEvent);
        }

        // POST: EducationEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,EventName,EventDate,EventTime,Speaker,Description")] EducationEvent educationEvent)
        {
            if (id != educationEvent.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(educationEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EducationEventExists(educationEvent.EventId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(educationEvent);
        }

        // GET: EducationEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var educationEvent = await _context.EducationEvents
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (educationEvent == null)
            {
                return NotFound();
            }

            return View(educationEvent);
        }

        // POST: EducationEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var educationEvent = await _context.EducationEvents.FindAsync(id);
            if (educationEvent != null)
            {
                _context.EducationEvents.Remove(educationEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EducationEventExists(int id)
        {
            return _context.EducationEvents.Any(e => e.EventId == id);
        }
    }
}
