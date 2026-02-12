using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using FortiTrafficAnalysis.Services.Authorization;
using FortiTrafficAnalysis.WebGui.Models;
using FortiTrafficAnalysis.Services.LogParsing;
using FortiTrafficAnalysis.Services;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize]
    public class TrafficAnalysisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrafficAnalysisController> _logger;
        private readonly IFortiGateLogParserService _logParser;
        private readonly ITicketNumberGenerator _ticketNumberGenerator;

        public TrafficAnalysisController(
            ApplicationDbContext context,
            ILogger<TrafficAnalysisController> logger,
            IFortiGateLogParserService logParser,
            ITicketNumberGenerator ticketNumberGenerator)
        {
            _context = context;
            _logger = logger;
            _logParser = logParser;
            _ticketNumberGenerator = ticketNumberGenerator;
        }

        // GET: TrafficAnalysis
        public async Task<IActionResult> Index()
        {
            var currentUserUPN = User.Identity?.Name;
            
            var analyses = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .Include(t => t.TrafficLogs)
                .Include(t => t.Recommendations)
                .Where(t => User.IsInRole("Admins") || t.CreatedByUPN == currentUserUPN)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(analyses);
        }

        // GET: TrafficAnalysis/Create
        public async Task<IActionResult> Create()
        {
            await LoadCustomersDropdown();
            return View();
        }

        // POST: TrafficAnalysis/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTrafficAnalysisViewModel viewModel)
        {
            _logger.LogInformation("Create POST called. Summary: {Summary}, FGID: {FGID}", viewModel.Summary, viewModel.FGID);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}", 
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
                await LoadCustomersDropdown();
                return View(viewModel);
            }

            // Get Customer and Service info from the selected FortiGate
            var fortiGate = await _context.FortiGates
                .Include(f => f.FTAService)
                    .ThenInclude(s => s.Customer)
                .FirstOrDefaultAsync(f => f.FGID == viewModel.FGID);

            if (fortiGate == null)
            {
                ModelState.AddModelError("FGID", "Selected FortiGate device not found.");
                await LoadCustomersDropdown();
                return View(viewModel);
            }

            var model = new TrafficAnalysis
            {
                TrafficAnalysisID = Guid.NewGuid(),
                TicketNumber = await GenerateUniqueTicketNumberAsync(),
                Summary = viewModel.Summary,
                Description = viewModel.Description,
                FGID = viewModel.FGID,
                FTAID = fortiGate.FTAID,
                CustomerID = fortiGate.FTAService?.CustomerID,
                CreatedByUPN = User.Identity?.Name ?? "Unknown",
                CreatedDate = DateTime.UtcNow,
                Status = "Open"
            };

            _context.TrafficAnalyses.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Traffic Analysis ticket '{model.Summary}' created successfully!";
            _logger.LogInformation("Traffic Analysis created: {TicketID} by {User}", 
                model.TrafficAnalysisID, User.Identity?.Name);

            return RedirectToAction(nameof(Details), new { id = model.TrafficAnalysisID });
        }

        // GET: TrafficAnalysis/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .Include(t => t.Customer)
                .Include(t => t.FTAService)
                .Include(t => t.TrafficLogs)
                .Include(t => t.Recommendations)
                .FirstOrDefaultAsync(m => m.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            return View(analysis);
        }

        // GET: TrafficAnalysis/GetServicesForCustomer
        [HttpGet]
        public async Task<IActionResult> GetServicesForCustomer(Guid customerId)
        {
            var services = await _context.FTAServices
                .Where(s => s.CustomerID == customerId)
                .OrderBy(s => s.JobID)
                .Select(s => new { value = s.FTAID, text = s.JobID })
                .ToListAsync();

            return Json(services);
        }

        // GET: TrafficAnalysis/GetFortiGatesForService
        [HttpGet]
        public async Task<IActionResult> GetFortiGatesForService(Guid serviceId)
        {
            var devices = await _context.FortiGates
                .Where(f => f.FTAID == serviceId)
                .OrderBy(f => f.FGHostname)
                .Select(f => new { value = f.FGID, text = f.FGHostname })
                .ToListAsync();

            return Json(devices);
        }

        // POST: TrafficAnalysis/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, string status)
        {
            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
                return NotFound();

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            analysis.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ticket status updated to '{status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: TrafficAnalysis/UploadLogFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogFile(Guid id, IFormFile logFile)
        {
            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                .FirstOrDefaultAsync(t => t.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            if (logFile == null || logFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a log file to upload.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".log", ".txt" };
            var fileExtension = Path.GetExtension(logFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only .log and .txt files are allowed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate file size (max 50MB)
            if (logFile.Length > 50 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size exceeds the maximum limit of 50MB.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                using (var stream = logFile.OpenReadStream())
                {
                    var logs = await _logParser.ParseLogFileAsync(stream, analysis.TrafficAnalysisID, analysis.FGID);

                    if (logs.Count == 0)
                    {
                        TempData["ErrorMessage"] = "No valid traffic logs found in the file.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    await _context.TrafficLogs.AddRangeAsync(logs);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Successfully imported {logs.Count} traffic log(s) from '{logFile.FileName}'.";
                    _logger.LogInformation("Imported {Count} logs for ticket {TicketID} by {User}", 
                        logs.Count, analysis.TrafficAnalysisID, User.Identity?.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading log file for ticket {TicketID}", analysis.TrafficAnalysisID);
                TempData["ErrorMessage"] = $"Error processing log file: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: TrafficAnalysis/GetLogs/5
        [HttpGet]
        public async Task<IActionResult> GetLogs(Guid id, string? search = null, string? filterAction = null, 
            string? srcIp = null, string? dstIp = null, string? proto = null)
        {
            _logger.LogInformation("GetLogs called for ticket {TicketID}", id);
            _logger.LogInformation("Parameters - search: '{Search}', filterAction: '{Action}', srcIp: '{SrcIp}', dstIp: '{DstIp}', proto: '{Proto}'", 
                search ?? "null", filterAction ?? "null", srcIp ?? "null", dstIp ?? "null", proto ?? "null");

            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
            {
                _logger.LogWarning("Ticket {TicketID} not found", id);
                return NotFound();
            }

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            var query = _context.TrafficLogs
                .Where(l => l.TrafficAnalysisID == id);

            var totalCount = await query.CountAsync();
            _logger.LogInformation("Found {Count} total logs for ticket {TicketID}", totalCount, id);

            // Apply filters ONLY if they have actual values (not null, not empty)
            if (!string.IsNullOrWhiteSpace(search))
            {
                _logger.LogInformation("Applying search filter: {Search}", search);
                query = query.Where(l => 
                    (l.SrcIP != null && l.SrcIP.Contains(search)) ||
                    (l.DstIP != null && l.DstIP.Contains(search)) ||
                    (l.SrcPort != null && l.SrcPort.Contains(search)) ||
                    (l.DstPort != null && l.DstPort.Contains(search)) ||
                    (l.Service != null && l.Service.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filterAction))
            {
                _logger.LogInformation("Applying action filter: {Action}", filterAction);
                query = query.Where(l => l.Action == filterAction);
            }

            if (!string.IsNullOrWhiteSpace(srcIp))
            {
                _logger.LogInformation("Applying srcIp filter: {SrcIp}", srcIp);
                query = query.Where(l => l.SrcIP != null && l.SrcIP.Contains(srcIp));
            }

            if (!string.IsNullOrWhiteSpace(dstIp))
            {
                _logger.LogInformation("Applying dstIp filter: {DstIp}", dstIp);
                query = query.Where(l => l.DstIP != null && l.DstIP.Contains(dstIp));
            }

            if (!string.IsNullOrWhiteSpace(proto))
            {
                _logger.LogInformation("Applying proto filter: {Proto}", proto);
                query = query.Where(l => l.Proto == proto);
            }

            var logs = await query
                .OrderByDescending(l => l.LogDate)
                .ThenByDescending(l => l.LogTime)
                .Select(l => new
                {
                    l.TrafficLogID,
                    l.LogDate,
                    l.LogTime,
                    l.LogId,
                    l.SrcIP,
                    l.SrcInt,
                    l.SrcPort,
                    l.DstIP,
                    l.DstInt,
                    l.DstPort,
                    l.Proto,
                    l.PolicyId,
                    l.Action,
                    l.Service,
                    l.SessionId,
                    l.PolicyName,
                    l.SentByte,
                    l.RcvdByte,
                    l.Duration
                })
                .ToListAsync();

            _logger.LogInformation("Returning {Count} logs after filtering", logs.Count);

            return Json(logs);
        }

        // GET: TrafficAnalysis/Delete/5
        [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                .Include(t => t.TrafficLogs)
                .Include(t => t.Recommendations)
                .FirstOrDefaultAsync(m => m.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            return View(analysis);
        }

        // POST: TrafficAnalysis/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var analysis = await _context.TrafficAnalyses
                .Include(t => t.TrafficLogs)
                .Include(t => t.Recommendations)
                .FirstOrDefaultAsync(t => t.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            _context.TrafficAnalyses.Remove(analysis);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Traffic Analysis ticket deleted successfully!";
            _logger.LogInformation("Traffic Analysis deleted: {TicketID} by {Admin}", 
                id, User.Identity?.Name);

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCustomersDropdown()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Customers = new SelectList(customers, "CustomerID", "CustomerName");
        }

        private async Task<string> GenerateUniqueTicketNumberAsync()
        {
            string ticketNumber;
            bool exists;

            do
            {
                ticketNumber = _ticketNumberGenerator.Generate();
                exists = await _context.TrafficAnalyses.AnyAsync(t => t.TicketNumber == ticketNumber);
            } while (exists);

            return ticketNumber;
        }
    }
}
