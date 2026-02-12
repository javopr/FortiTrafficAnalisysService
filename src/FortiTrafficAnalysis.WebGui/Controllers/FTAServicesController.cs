using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using FortiTrafficAnalysis.WebGui.Models;
using FortiTrafficAnalysis.Services.Authorization;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public class FTAServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FTAServicesController> _logger;

        public FTAServicesController(
            ApplicationDbContext context,
            ILogger<FTAServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: FTAServices
        public async Task<IActionResult> Index()
        {
            var services = await _context.FTAServices
                .Include(s => s.Customer)
                .Include(s => s.FortiGates)
                .Select(s => new FTAServiceViewModel
                {
                    FTAID = s.FTAID,
                    JobID = s.JobID,
                    CustomerID = s.CustomerID,
                    CustomerName = s.Customer.CustomerName,
                    ServiceStart = s.ServiceStart,
                    ServiceEnd = s.ServiceEnd,
                    ServiceStatus = s.ServiceStatus,
                    DeviceCount = s.FortiGates.Count
                })
                .OrderByDescending(s => s.ServiceStart)
                .ToListAsync();

            return View(services);
        }

        // GET: FTAServices/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.FTAServices
                .Include(s => s.Customer)
                .Include(s => s.FortiGates)
                .FirstOrDefaultAsync(m => m.FTAID == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // GET: FTAServices/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerID", "CustomerName");
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Pending", "Expired" });
            
            var model = new FTAServiceViewModel
            {
                ServiceStart = DateTime.Today,
                ServiceEnd = DateTime.Today.AddYears(1),
                ServiceStatus = "Active"
            };

            return View(model);
        }

        // POST: FTAServices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FTAServiceViewModel model)
        {
            if (model.ServiceEnd <= model.ServiceStart)
            {
                ModelState.AddModelError("ServiceEnd", "Service end date must be after start date.");
            }

            if (await _context.FTAServices.AnyAsync(s => s.JobID.ToLower() == model.JobID.ToLower()))
            {
                ModelState.AddModelError("JobID", "A service with this Job ID already exists.");
            }

            if (ModelState.IsValid)
            {
                var service = new FTAService
                {
                    JobID = model.JobID,
                    CustomerID = model.CustomerID,
                    ServiceStart = model.ServiceStart,
                    ServiceEnd = model.ServiceEnd,
                    ServiceStatus = model.ServiceStatus
                };

                _context.Add(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Service '{service.JobID}' created successfully!";
                _logger.LogInformation("FTA Service created: {JobID} by {Admin}", service.JobID, User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerID", "CustomerName", model.CustomerID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Pending", "Expired" }, model.ServiceStatus);
            return View(model);
        }

        // GET: FTAServices/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.FTAServices.FindAsync(id);
            if (service == null)
                return NotFound();

            var model = new FTAServiceViewModel
            {
                FTAID = service.FTAID,
                JobID = service.JobID,
                CustomerID = service.CustomerID,
                ServiceStart = service.ServiceStart,
                ServiceEnd = service.ServiceEnd,
                ServiceStatus = service.ServiceStatus
            };

            ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerID", "CustomerName", service.CustomerID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Pending", "Expired" }, service.ServiceStatus);
            return View(model);
        }

        // POST: FTAServices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FTAServiceViewModel model)
        {
            if (id != model.FTAID)
                return NotFound();

            if (model.ServiceEnd <= model.ServiceStart)
            {
                ModelState.AddModelError("ServiceEnd", "Service end date must be after start date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var service = await _context.FTAServices.FindAsync(id);
                    if (service == null)
                        return NotFound();

                    service.JobID = model.JobID;
                    service.CustomerID = model.CustomerID;
                    service.ServiceStart = model.ServiceStart;
                    service.ServiceEnd = model.ServiceEnd;
                    service.ServiceStatus = model.ServiceStatus;

                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Service '{service.JobID}' updated successfully!";
                    _logger.LogInformation("FTA Service updated: {JobID} by {Admin}", service.JobID, User.Identity.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerID", "CustomerName", model.CustomerID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Pending", "Expired" }, model.ServiceStatus);
            return View(model);
        }

        // GET: FTAServices/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.FTAServices
                .Include(s => s.Customer)
                .Include(s => s.FortiGates)
                .FirstOrDefaultAsync(m => m.FTAID == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: FTAServices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var service = await _context.FTAServices
                .Include(s => s.FortiGates)
                .FirstOrDefaultAsync(s => s.FTAID == id);

            if (service == null)
                return NotFound();

            if (service.FortiGates.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete service '{service.JobID}' because it has {service.FortiGates.Count} associated device(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.FTAServices.Remove(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Service '{service.JobID}' deleted successfully!";
            _logger.LogInformation("FTA Service deleted: {JobID} by {Admin}", service.JobID, User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(Guid id)
        {
            return _context.FTAServices.Any(e => e.FTAID == id);
        }
    }
}
