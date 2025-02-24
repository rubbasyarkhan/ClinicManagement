using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EducationEventsController : Controller
    {
        private readonly ClinicManagementDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EducationEventsController(ClinicManagementDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: EducationEvents
        public async Task<IActionResult> Index()
        {
            return View(await _context.EducationEvents.ToListAsync());
        }

        // GET: EducationEvents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EducationEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,EventName,EventDate,EventTime,Speaker,Description")] EducationEvent educationEvent, IFormFile EventImage)
        {
            if (ModelState.IsValid)
            {
                if (EventImage != null)
                {
                    string uniqueFileName = UploadImage(EventImage);
                    educationEvent.EventImage = uniqueFileName;
                }
                _context.Add(educationEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(educationEvent);
        }

        // GET: EducationEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var educationEvent = await _context.EducationEvents.FindAsync(id);
            if (educationEvent == null) return NotFound();

            return View(educationEvent);
        }

        // POST: EducationEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
  
        public async Task<IActionResult> Edit(int id, EducationEvent model, IFormFile? EventImage)
        {
            if (id != model.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var eventToUpdate = await _context.EducationEvents.FindAsync(id);
                    if (eventToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Update event details
                    eventToUpdate.EventName = model.EventName;
                    eventToUpdate.EventDate = model.EventDate;
                    eventToUpdate.EventTime = model.EventTime;
                    eventToUpdate.Speaker = model.Speaker;
                    eventToUpdate.Description = model.Description;

                    // Handle new image upload
                    if (EventImage != null && EventImage.Length > 0)
                    {
                        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/events");

                        // Ensure the directory exists
                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        // Delete the old image if it exists
                        if (!string.IsNullOrEmpty(eventToUpdate.EventImage))
                        {
                            var oldImagePath = Path.Combine(uploadFolder, eventToUpdate.EventImage);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save the new image
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(EventImage.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await EventImage.CopyToAsync(stream);
                        }

                        eventToUpdate.EventImage = fileName; // Save only the filename
                    }

                    _context.Update(eventToUpdate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating the event. Please try again.");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid event ID.");
            }

            var educationEvent = await _context.EducationEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (educationEvent == null)
            {
                return NotFound($"No event found with ID {id}.");
            }

            return View(educationEvent);
        }



        // GET: EducationEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var educationEvent = await _context.EducationEvents.FirstOrDefaultAsync(m => m.EventId == id);
            if (educationEvent == null) return NotFound();

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
                // Delete the event image from the server if it exists
                if (!string.IsNullOrEmpty(educationEvent.EventImage))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, educationEvent.EventImage);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.EducationEvents.Remove(educationEvent);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EducationEventExists(int id)
        {
            return _context.EducationEvents.Any(e => e.EventId == id);
        }

        private string UploadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // Ensure the upload directory exists
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "image/events");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate a unique filename
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            return "image/events/" + uniqueFileName; // Relative path to store in DB
        }


    }
}
