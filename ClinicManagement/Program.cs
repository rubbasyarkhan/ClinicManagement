using ClinicManagement.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Database Context
builder.Services.AddDbContext<ClinicManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Enable Session Management
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// 🔹 Add Authentication & Cookie-based Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Redirect if not logged in
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect if unauthorized
        options.ExpireTimeSpan = TimeSpan.FromHours(1); // Set session timeout
    });

// 🔹 Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// 🔹 Enable Authentication & Authorization Middleware
app.UseAuthentication(); // Ensures authentication before authorization
app.UseAuthorization();

// ✅ Corrected Admin Routes
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

// ✅ Default Client Routes (Home Page → Client Controller)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Client}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "eventRegistrations",
    pattern: "EventRegistrations/{action}/{id?}",
    defaults: new { controller = "EventRegistrations" });

app.Run();
