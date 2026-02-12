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
    public class FortiGatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FortiGatesController> _logger;

        public FortiGatesController(
            ApplicationDbContext context,
            ILogger<FortiGatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: FortiGates
        public async Task<IActionResult> Index()
        {
            var devices = await _context.FortiGates
                .Include(f => f.FTAService)
                    .ThenInclude(s => s.Customer)
                .Include(f => f.TrafficLogs)
                .Select(f => new FortiGateViewModel
                {
                    FGID = f.FGID,
                    FTAID = f.FTAID,
                    ServiceJobID = f.FTAService.JobID,
                    CustomerName = f.FTAService.Customer.CustomerName,
                    FGHostname = f.FGHostname,
                    FGHost = f.FGHost,
                    FGSerial = f.FGSerial,
                    FGvDOM = f.FGvDOM,
                    FGStatus = f.FGStatus,
                    LogCount = f.TrafficLogs.Count
                })
                .OrderBy(f => f.CustomerName)
                .ThenBy(f => f.FGHostname)
                .ToListAsync();

            return View(devices);
        }

        // GET: FortiGates/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var device = await _context.FortiGates
                .Include(f => f.FTAService)
                    .ThenInclude(s => s.Customer)
                .Include(f => f.TrafficLogs)
                .FirstOrDefaultAsync(m => m.FGID == id);

            if (device == null)
                return NotFound();

            return View(device);
        }

        // GET: FortiGates/Create
        public async Task<IActionResult> Create()
        {
            var services = await _context.FTAServices
                .Include(s => s.Customer)
                .OrderBy(s => s.Customer.CustomerName)
                .ThenBy(s => s.JobID)
                .Select(s => new
                {
                    s.FTAID,
                    DisplayText = $"{s.Customer.CustomerName} - {s.JobID}"
                })
                .ToListAsync();

            ViewBag.Services = new SelectList(services, "FTAID", "DisplayText");
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Maintenance", "Offline" });

            var model = new CreateFortiGateViewModel
            {
                FGvDOM = "root",
                FGStatus = "Active"
            };

            return View(model);
        }

        // POST: FortiGates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFortiGateViewModel model)
        {
            if (await _context.FortiGates.AnyAsync(f => f.FGSerial.ToLower() == model.FGSerial.ToLower()))
            {
                ModelState.AddModelError("FGSerial", "A device with this serial number already exists.");
            }

            if (ModelState.IsValid)
            {
                var device = new FortiGate
                {
                    FTAID = model.FTAID,
                    FGHostname = model.FGHostname,
                    FGHost = model.FGHost,
                    FGSerial = model.FGSerial,
                    FGvDOM = model.FGvDOM,
                    FGapiKey = model.FGapiKey,
                    FGStatus = model.FGStatus
                };

                _context.Add(device);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"FortiGate device '{device.FGHostname}' created successfully!";
                _logger.LogInformation("FortiGate created: {Hostname} ({Serial}) by {Admin}", 
                    device.FGHostname, device.FGSerial, User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }

            var services = await _context.FTAServices
                .Include(s => s.Customer)
                .OrderBy(s => s.Customer.CustomerName)
                .ThenBy(s => s.JobID)
                .Select(s => new
                {
                    s.FTAID,
                    DisplayText = $"{s.Customer.CustomerName} - {s.JobID}"
                })
                .ToListAsync();

            ViewBag.Services = new SelectList(services, "FTAID", "DisplayText", model.FTAID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Maintenance", "Offline" }, model.FGStatus);
            return View(model);
        }

        // GET: FortiGates/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var device = await _context.FortiGates.FindAsync(id);
            if (device == null)
                return NotFound();

            var model = new FortiGateViewModel
            {
                FGID = device.FGID,
                FTAID = device.FTAID,
                FGHostname = device.FGHostname,
                FGHost = device.FGHost,
                FGSerial = device.FGSerial,
                FGvDOM = device.FGvDOM,
                FGapiKey = device.FGapiKey,
                FGStatus = device.FGStatus
            };

            var services = await _context.FTAServices
                .Include(s => s.Customer)
                .OrderBy(s => s.Customer.CustomerName)
                .ThenBy(s => s.JobID)
                .Select(s => new
                {
                    s.FTAID,
                    DisplayText = $"{s.Customer.CustomerName} - {s.JobID}"
                })
                .ToListAsync();

            ViewBag.Services = new SelectList(services, "FTAID", "DisplayText", device.FTAID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Maintenance", "Offline" }, device.FGStatus);
            return View(model);
        }

        // POST: FortiGates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FortiGateViewModel model)
        {
            if (id != model.FGID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var device = await _context.FortiGates.FindAsync(id);
                    if (device == null)
                        return NotFound();

                    device.FTAID = model.FTAID;
                    device.FGHostname = model.FGHostname;
                    device.FGHost = model.FGHost;
                    device.FGSerial = model.FGSerial;
                    device.FGvDOM = model.FGvDOM;
                    device.FGStatus = model.FGStatus;

                    // Only update API key if a new one is provided
                    if (!string.IsNullOrEmpty(model.FGapiKey))
                    {
                        device.FGapiKey = model.FGapiKey;
                    }

                    _context.Update(device);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"FortiGate device '{device.FGHostname}' updated successfully!";
                    _logger.LogInformation("FortiGate updated: {Hostname} ({Serial}) by {Admin}", 
                        device.FGHostname, device.FGSerial, User.Identity.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var services = await _context.FTAServices
                .Include(s => s.Customer)
                .OrderBy(s => s.Customer.CustomerName)
                .ThenBy(s => s.JobID)
                .Select(s => new
                {
                    s.FTAID,
                    DisplayText = $"{s.Customer.CustomerName} - {s.JobID}"
                })
                .ToListAsync();

            ViewBag.Services = new SelectList(services, "FTAID", "DisplayText", model.FTAID);
            ViewBag.StatusOptions = new SelectList(new[] { "Active", "Inactive", "Maintenance", "Offline" }, model.FGStatus);
            return View(model);
        }

        // GET: FortiGates/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var device = await _context.FortiGates
                .Include(f => f.FTAService)
                    .ThenInclude(s => s.Customer)
                .Include(f => f.TrafficLogs)
                .FirstOrDefaultAsync(m => m.FGID == id);

            if (device == null)
                return NotFound();

            return View(device);
        }

        // POST: FortiGates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var device = await _context.FortiGates
                .Include(f => f.TrafficLogs)
                .FirstOrDefaultAsync(f => f.FGID == id);

            if (device == null)
                return NotFound();

            if (device.TrafficLogs.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete device '{device.FGHostname}' because it has {device.TrafficLogs.Count} associated traffic log(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.FortiGates.Remove(device);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"FortiGate device '{device.FGHostname}' deleted successfully!";
            _logger.LogInformation("FortiGate deleted: {Hostname} ({Serial}) by {Admin}", 
                device.FGHostname, device.FGSerial, User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        private bool DeviceExists(Guid id)
        {
            return _context.FortiGates.Any(e => e.FGID == id);
        }
    }
}
