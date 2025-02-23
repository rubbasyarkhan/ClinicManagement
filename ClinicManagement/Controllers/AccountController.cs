using ClinicManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly ClinicManagementDbContext _context;

        public AccountController(ClinicManagementDbContext context)
        {
            _context = context;
        }

        // ✅ Render the Login Page
        public IActionResult Login()
        {
            return View();
        }

        // ✅ Handle Login POST Request
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password && u.Role == "Admin");

            if (admin == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            // 🔹 Create Authentication Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin") // Ensure this role matches your DB role
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // 🔹 Sign In the Admin
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                          new ClaimsPrincipal(claimsIdentity),
                                          authProperties);

            return RedirectToAction("Index", "Admin"); // Redirect to Admin Panel
        }

        // ✅ Handle Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
