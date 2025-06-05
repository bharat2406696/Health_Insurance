// Controllers/EmployeeController.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net; // Add this using statement for password hashing

namespace Health_Insurance.Controllers
{
    // Restrict all actions in this controller to users with the "Admin" role.
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Employee/Index
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees.Include(e => e.Organization).ToListAsync();
            return View(employees);
        }

        // GET: /Employee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: /Employee/Create
        public IActionResult Create()
        {
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName");
            return View();
        }

        // POST: /Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Removed "Organization" from the [Bind] attribute.
        // We will manually load the Organization object after model binding.
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,Designation,OrganizationId,Username,PasswordHash")] Employee employee)
        {
            // Hash the password before saving a new employee
            if (!string.IsNullOrEmpty(employee.PasswordHash))
            {
                employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(employee.PasswordHash);
            }
            else
            {
                // If PasswordHash is empty, and it's required for new user creation,
                // add a model error.
                ModelState.AddModelError(nameof(employee.PasswordHash), "Password is required for new employees.");
            }

            // Check if OrganizationId is valid before proceeding
            // This is a redundant check if OrganizationId is [Required] and ModelState.IsValid is used,
            // but provides clarity.
            if (employee.OrganizationId <= 0)
            {
                ModelState.AddModelError(nameof(employee.OrganizationId), "Please select a valid organization.");
            }

            if (ModelState.IsValid) // This will check all [Required] fields, excluding the Organization navigation property
            {
                // Manually retrieve and assign the Organization navigation property
                // This prevents model binding from trying to create or validate the complex object directly.
                var organization = await _context.Organizations.FindAsync(employee.OrganizationId);
                if (organization == null)
                {
                    ModelState.AddModelError(nameof(employee.OrganizationId), "Selected organization not found.");
                    ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", employee.OrganizationId);
                    return View(employee);
                }
                employee.Organization = organization; // Assign the loaded organization

                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, re-populate ViewData for dropdown and return view
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", employee.OrganizationId);
            return View(employee);
        }

        // GET: /Employee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Map Employee model to EmployeeEditViewModel for the view
            var viewModel = new EmployeeEditViewModel
            {
                EmployeeId = employee.EmployeeId,
                Name = employee.Name,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address,
                Designation = employee.Designation,
                OrganizationId = employee.OrganizationId,
                Username = employee.Username,
                // Do NOT set Password here, as it's sensitive and not stored in plain text.
                // The ViewModel's Password property is for new input only.
                Password = null // Ensure it's empty so the field is blank for optional update
            };

            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", employee.OrganizationId);
            return View(viewModel); // Pass the ViewModel to the view
        }

        // POST: /Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeEditViewModel viewModel) // Accept EmployeeEditViewModel
        {
            if (id != viewModel.EmployeeId)
            {
                return NotFound();
            }

            // Manually remove Password from ModelState validation if it's empty
            // This is crucial because EmployeeEditViewModel.Password is NOT [Required]
            // but client-side or other validation might still flag it if it's the only issue.
            if (ModelState.ContainsKey(nameof(viewModel.Password)) && string.IsNullOrEmpty(viewModel.Password))
            {
                ModelState.Remove(nameof(viewModel.Password));
            }

            if (ModelState.IsValid)
            {
                // Retrieve the existing employee from the database
                var employeeToUpdate = await _context.Employees.FindAsync(id);

                if (employeeToUpdate == null)
                {
                    return NotFound(); // Employee not found in DB
                }

                // Update properties from the ViewModel to the existing employee entity
                employeeToUpdate.Name = viewModel.Name;
                employeeToUpdate.Email = viewModel.Email;
                employeeToUpdate.Phone = viewModel.Phone;
                employeeToUpdate.Address = viewModel.Address;
                employeeToUpdate.Designation = viewModel.Designation;
                employeeToUpdate.OrganizationId = viewModel.OrganizationId;
                employeeToUpdate.Username = viewModel.Username; // Update username if changed

                // Only update PasswordHash if a new password was provided in the ViewModel
                if (!string.IsNullOrEmpty(viewModel.Password))
                {
                    employeeToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(viewModel.Password);
                }
                // If viewModel.Password is null/empty, the existing employeeToUpdate.PasswordHash remains unchanged.


                try
                {
                    _context.Update(employeeToUpdate); // Attach and mark as modified
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(viewModel.EmployeeId)) // Use viewModel.EmployeeId here
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

            // If ModelState is not valid, re-populate ViewData for dropdown and return view
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", viewModel.OrganizationId);
            return View(viewModel); // Pass the ViewModel back to the view
        }

        // GET: /Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: /Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
