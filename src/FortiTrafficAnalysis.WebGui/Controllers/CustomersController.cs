using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using FortiTrafficAnalysis.WebGui.Models;
using FortiTrafficAnalysis.Services.Authorization;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(
            ApplicationDbContext context,
            ILogger<CustomersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .Include(c => c.FTAServices)
                .Select(c => new CustomerViewModel
                {
                    CustomerID = c.CustomerID,
                    CustomerName = c.CustomerName,
                    ServiceCount = c.FTAServices.Count
                })
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers
                .Include(c => c.FTAServices)
                .FirstOrDefaultAsync(m => m.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = new Customer
                {
                    CustomerName = model.CustomerName
                };

                _context.Add(customer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully!";
                _logger.LogInformation("Customer created: {CustomerName} by {User}", customer.CustomerName, User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            var model = new CustomerViewModel
            {
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName
            };

            return View(model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CustomerViewModel model)
        {
            if (id != model.CustomerID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _context.Customers.FindAsync(id);
                    if (customer == null)
                        return NotFound();

                    customer.CustomerName = model.CustomerName;
                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully!";
                    _logger.LogInformation("Customer updated: {CustomerName} by {User}", customer.CustomerName, User.Identity.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers
                .Include(c => c.FTAServices)
                .FirstOrDefaultAsync(m => m.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var customer = await _context.Customers
                .Include(c => c.FTAServices)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return NotFound();

            if (customer.FTAServices.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete customer '{customer.CustomerName}' because it has {customer.FTAServices.Count} associated service(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' deleted successfully!";
            _logger.LogInformation("Customer deleted: {CustomerName} by {User}", customer.CustomerName, User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(Guid id)
        {
            return _context.Customers.Any(e => e.CustomerID == id);
        }
    }
}
