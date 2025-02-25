using ClinicManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ClinicManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public AuthController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // 🔹 Register View
        public IActionResult Register()
        {
            return View();
        }

        // 🔹 Register Logic
        [HttpPost]
        public IActionResult Register(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already registered!";
                return View();
            }

            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ViewBag.Error = "Username is already taken!";
                return View();
            }

            user.Password = HashPassword(user.Password); // Hash password before saving
            user.Role = "User"; // Default role

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }

        // 🔹 Login View
        public IActionResult Login()
        {
            return View();
        }

        // 🔹 Login Logic
        [HttpPost]
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.Password != HashPassword(password))
            {
                ViewBag.Error = "Invalid Email or Password!";
                return View();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Ensure this is set
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync(principal);

            return RedirectToAction("Index", "Client");
        }


        // 🔹 Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 🔹 Password Hashing
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
