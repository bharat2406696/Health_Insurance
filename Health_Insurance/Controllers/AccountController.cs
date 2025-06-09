// Controllers/AccountController.cs
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;

namespace Health_Insurance.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Employee"); // Admin goes to Employee management
                }
                else if (User.IsInRole("Employee"))
                {
                    var employeeIdClaim = User.FindFirst("EmployeeId");
                    if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
                    {
                        return RedirectToAction("EnrolledPolicies", "Enrollment", new { employeeId = employeeId });
                    }
                    return RedirectToAction("Index", "Home");
                }
                // --- NEW: Redirect HR users ---
                else if (User.IsInRole("HR"))
                {
                    // HR personnel might also manage employees, so redirect them to Employee/Index
                    return RedirectToAction("Index", "Employee");
                }
                // --- END NEW ---
                return RedirectToAction("Index", "Home"); // Fallback
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.AuthenticateUserAsync(model.Username, model.Password, model.LoginType);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password, or incorrect login type selected.");
                return View(model);
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.Name, model.Username),
            };

            string userRole = string.Empty;
            int userId = 0; // To store AdminId, EmployeeId, or HRId

            if (user is Admin adminUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new System.Security.Claims.Claim("AdminId", adminUser.AdminId.ToString()));
                userRole = "Admin";
                userId = adminUser.AdminId;
            }
            else if (user is Employee employeeUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "Employee"));
                claims.Add(new System.Security.Claims.Claim("EmployeeId", employeeUser.EmployeeId.ToString()));
                userRole = "Employee";
                userId = employeeUser.EmployeeId;
            }
            // --- NEW: Handle HR claims and role ---
            else if (user is HR hrUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "HR"));
                claims.Add(new System.Security.Claims.Claim("HRId", hrUser.HRId.ToString())); // Custom claim for HRId
                userRole = "HR";
                userId = hrUser.HRId;
            }
            // --- END NEW ---

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = System.DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect based on role after successful login
            if (userRole == "Admin")
            {
                return RedirectToAction("Index", "Employee");
            }
            else if (userRole == "Employee")
            {
                return RedirectToAction("EnrolledPolicies", "Enrollment", new { employeeId = userId });
            }
            // --- NEW: Redirect HR personnel after login ---
            else if (userRole == "HR")
            {
                return RedirectToAction("Index", "Employee"); // HR also goes to Employee management initially
            }
            // --- END NEW ---
            return RedirectToAction("Index", "Home"); // Fallback redirect
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

