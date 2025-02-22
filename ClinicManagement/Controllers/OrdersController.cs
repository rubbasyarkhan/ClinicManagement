using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public OrdersController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // GET: Orders (With Filters)
        public IActionResult Index(string status, string userName, DateTime? startDate, DateTime? endDate)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                ordersQuery = ordersQuery.Where(o => o.User != null && o.User.FullName.Contains(userName));
            }

            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value);
            }

            var orders = ordersQuery.ToList();

            // Ensure TotalAmount Calculation
            foreach (var order in orders)
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
            }

            // Calculate Total Revenue for Delivered Orders
            ViewBag.TotalRevenue = orders
                .Where(o => o.OrderStatus == "Delivered")
                .Sum(o => o.TotalAmount);

            // Pass Filters to View
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedUserName = userName;
            ViewBag.SelectedStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedEndDate = endDate?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // GET: Orders/Details/5
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Ensure TotalAmount Calculation
            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            return View(order);
        }

        // POST: Update Order Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (id == 0 || string.IsNullOrEmpty(status))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                order.OrderStatus = status;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order status updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update order status. " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Ensure product details are included
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order); // Redirect to the Delete.cshtml view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting order: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult PrintBill(int id)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Ensure TotalAmount Calculation
            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            return View(order);
        }



        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
