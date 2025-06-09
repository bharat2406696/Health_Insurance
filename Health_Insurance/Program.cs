// Program.cs
using Health_Insurance.Data; // Ensure this namespace is correct for ApplicationDbContext
using Health_Insurance.Services; // Ensure this namespace is correct for your Services (IUserService, UserService, IClaimService, ClaimService, IEnrollmentService, EnrollmentService, IPremiumCalculatorService, PremiumCalculatorService, IReportService, ReportService)
using Microsoft.EntityFrameworkCore; // For UseSqlServer
using Microsoft.AspNetCore.Authentication.Cookies; // For CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authorization; // For Authorization policies (if needed later)
using Microsoft.AspNetCore.Mvc.Authorization; // For AuthorizeFilter (if needed later)

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQL Server database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Authentication (Cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Path to your login page
        options.LogoutPath = "/Account/Logout"; // Path to your logout action
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path for access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie expiration
        options.SlidingExpiration = true; // Renew cookie on activity
    });

// Register your custom services for Dependency Injection
// User Service
builder.Services.AddScoped<IUserService, UserService>();
// Claim Service
builder.Services.AddScoped<IClaimService, ClaimService>();
// Enrollment Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
// Premium Calculator Service
builder.Services.AddScoped<IPremiumCalculatorService, PremiumCalculatorService>();
// --- NEW: Register Report Service ---
builder.Services.AddScoped<IReportService, ReportService>();
// --- END NEW ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable Authentication middleware
app.UseAuthentication();
// Enable Authorization middleware
app.UseAuthorization();

// Map controllers to routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Changed default route to Account/Login

app.Run();


