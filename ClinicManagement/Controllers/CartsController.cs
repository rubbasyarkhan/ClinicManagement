using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ClinicManagement.Models;
using ClinicManagement.ViewModels;
using Newtonsoft.Json;

public class CartController : Controller
{
    private readonly ClinicManagementDbContext _context;

    public CartController(ClinicManagementDbContext context)
    {
        _context = context;
    }

    // GET: View Cart
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        int parsedUserId = int.Parse(userId);
        var cartItems = await _context.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId == parsedUserId)
            .ToListAsync();

        return View(cartItems);
    }
    // GET: Cart Item Count
    [Authorize]
    public IActionResult GetCartCount()
    {
        int cartCount = 0;

        if (HttpContext.Session.GetString("Cart") != null)
        {
            var cart = JsonConvert.DeserializeObject<List<CartItem>>(HttpContext.Session.GetString("Cart"));
            cartCount = cart.Sum(item => item.Quantity);
        }

        return Json(new { cartCount });
    }


    // POST: Add Product to Cart
    // POST: Add Product to Cart
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity)
    {
        if (quantity <= 0) return BadRequest("Quantity must be greater than zero.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        int parsedUserId = int.Parse(userId);
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound("Product not found.");

        var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == parsedUserId && c.ProductId == productId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            var newCartItem = new Cart
            {
                UserId = parsedUserId,
                ProductId = productId,
                Quantity = quantity,
                AddedDate = DateTime.Now
            };
            await _context.Carts.AddAsync(newCartItem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Cart");
    }

    // POST: Remove Item from Cart
    // POST: Remove Item from Cart
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int cartId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        int parsedUserId = int.Parse(userId);
        var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == parsedUserId);

        if (cartItem == null) return NotFound("Cart item not found.");

        _context.Carts.Remove(cartItem);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // POST: Clear Cart

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        int parsedUserId = int.Parse(userId);
        var cartItems = await _context.Carts.Where(c => c.UserId == parsedUserId).ToListAsync();

        _context.Carts.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // POST: Update Cart Quantity
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
    {
        if (quantity <= 0) return BadRequest("Quantity must be greater than zero.");

        var cartItem = await _context.Carts.Include(c => c.Product).FirstOrDefaultAsync(c => c.CartId == cartId);
        if (cartItem == null) return NotFound("Cart item not found.");

        cartItem.Quantity = quantity;
        await _context.SaveChangesAsync();

        decimal updatedTotal = cartItem.Quantity * cartItem.Product.Price;
        decimal cartTotal = await _context.Carts
            .Where(c => c.UserId == cartItem.UserId)
            .SumAsync(c => c.Quantity * c.Product.Price);

        return Json(new { success = true, newTotal = updatedTotal, cartTotal });
    }

    // GET: Checkout Page
    [Authorize]
    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var cartItems = await _context.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId.ToString() == userId)
            .ToListAsync();

        if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

        var checkoutViewModel = new CheckoutViewModel
        {
            CartItems = cartItems,
            TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price),
            OrderDetails = cartItems.Select(c => new OrderDetailViewModel
            {
                ProductId = (int)c.ProductId,
                Quantity = c.Quantity,
                Price = c.Product.Price
            }).ToList()
        };

        return View(checkoutViewModel);
    }


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel order)
    {
        if (order?.OrderDetails == null || !order.OrderDetails.Any())
            return BadRequest("Invalid order data");

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int parsedUserId))
            return Unauthorized();

        // ✅ Ensure TotalAmount is correctly calculated
        decimal totalAmount = order.OrderDetails.Sum(item => item.Price * item.Quantity);

        if (totalAmount <= 0)
            return BadRequest("Total amount must be greater than zero.");

        var newOrder = new Order
        {
            UserId = parsedUserId,
            OrderDate = DateTime.Now,
            TotalAmount = totalAmount,  // Use dynamically calculated total
            ShippingAddress = order.ShippingAddress,
            OrderStatus = "Pending",
            PaymentMethod = order.PaymentMethod
        };

        await _context.Orders.AddAsync(newOrder);
        await _context.SaveChangesAsync();

        var orderDetails = order.OrderDetails.Select(item => new OrderDetail
        {
            OrderId = newOrder.OrderId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();

        await _context.OrderDetails.AddRangeAsync(orderDetails);
        await _context.SaveChangesAsync();

        // ✅ Clear cart after placing order
        var cartItems = await _context.Carts.Where(c => c.UserId == parsedUserId).ToListAsync();
        if (cartItems.Any())
        {
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("OrderConfirmation", new { orderId = newOrder.OrderId });
    }

    [Authorize]
    public async Task<IActionResult> OrderConfirmation(int orderId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) return NotFound("Order not found.");

        return View(order);
    }

    [Authorize]
    public async Task<IActionResult> OrderHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user ID
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int parsedUserId = int.Parse(userId); // Convert string to int

        var orders = await _context.Orders
            .Where(o => o.UserId == parsedUserId) // Fix typo: 'UserI' → 'UserId'
                                                  // Fix typo: 'UserI' → 'UserId'
            .Include(o => o.OrderDetails) // Fix: 'OrderItems' → 'OrderDetails' (as per model)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var orderHistory = orders.Select(o => new OrderHistoryViewModel
        {
            OrderID = o.OrderId, // Fix: 'OrderID' → 'OrderId' (as per model)
            OrderDate = o.OrderDate.GetValueOrDefault(), // Converts nullable DateTime? to DateTime safely

            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus,
            Items = o.OrderDetails.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList()
        }).ToList();

        return View(orderHistory);
    }



}
