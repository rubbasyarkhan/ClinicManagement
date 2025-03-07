using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        // GET: Orders - Accessible by Admin and Clients
        public async Task<IActionResult> Index(string status, string userName, DateTime? startDate, DateTime? endDate)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "User";
            var userId = User.Identity.Name;

            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            // Clients can only see their own orders
            if (userRole == "User")
            {
                ordersQuery = ordersQuery.Where(o => o.User.Username == userId);
            }

            // Apply Filters (Admin Only)
            if (userRole == "Admin")
            {
                if (!string.IsNullOrEmpty(status))
                    ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);

                if (!string.IsNullOrEmpty(userName))
                    ordersQuery = ordersQuery.Where(o => o.User.FullName.Contains(userName));

                if (startDate.HasValue)
                    ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value);
            }

            var orders = await ordersQuery.ToListAsync();

            // Calculate TotalAmount per Order
            foreach (var order in orders)
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
            }

            ViewBag.UserRole = userRole;
            return View(orders);
        }

        // GET: Order Details - Accessible by Admin & Clients (Restricted to their orders)
        public async Task<IActionResult> Details(int id)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "User";
            var userId = User.Identity.Name;

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || (userRole == "User" && order.User.Username != userId))
                return NotFound();

            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
            ViewBag.UserRole = userRole;
            return View(order);
        }

        // Admin - Update Order Status
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            order.OrderStatus = status;

            if (status == "Delivered")
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    var product = orderDetail.Product;
                    if (product.StockQuantity >= orderDetail.Quantity)
                        product.StockQuantity -= orderDetail.Quantity;
                    else
                        return BadRequest("Not enough stock for " + product.Name);
                }
                _context.Products.UpdateRange(order.OrderDetails.Select(od => od.Product));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Client - Place an Order
        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
                return Json(new { success = false, message = "Invalid order data." });

            var user = await _context.Users.FindAsync(order.UserId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var productIds = order.OrderDetails.Select(od => od.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();

            foreach (var orderDetail in order.OrderDetails)
            {
                var product = products.FirstOrDefault(p => p.ProductId == orderDetail.ProductId);
                if (product == null || product.StockQuantity < orderDetail.Quantity)
                    return Json(new { success = false, message = $"Not enough stock for {product?.Name ?? "one of the products"}." });

                product.StockQuantity -= orderDetail.Quantity;
            }

            order.OrderDate = DateTime.Now;
            order.OrderStatus = "Pending";
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Order placed successfully!" });
        }

        // Admin - Delete Order
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
                return NotFound();

            if (order.OrderStatus == "Canceled")
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    var product = orderDetail.Product;
                    product.StockQuantity += orderDetail.Quantity;
                }
                _context.Products.UpdateRange(order.OrderDetails.Select(od => od.Product));
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // Admin & Client - Print Order Bill
        public async Task<IActionResult> PrintBill(int id)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "User";
            var userId = User.Identity.Name;

            var order = await _context.Orders
                .Include(o => o.User) // Ensure User is included
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || (userRole == "User" && order.User?.Username != userId))
                return NotFound();

            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            // Pass the user's phone number to the view
            ViewBag.UserPhoneNumber = order.User?.PhoneNumber;

            return View(order);
        }

    }
}
