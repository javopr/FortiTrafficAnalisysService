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
using FortiTrafficAnalysis.Services.Recommendations;
using FortiTrafficAnalysis.Services.AI;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize]
    public class TrafficAnalysisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrafficAnalysisController> _logger;
        private readonly IFortiGateLogParserService _logParser;
        private readonly ITicketNumberGenerator _ticketNumberGenerator;
        private readonly IPolicyRecommendationService _recommendationService;
        private readonly IAIRecommendationService _aiService;

        public TrafficAnalysisController(
            ApplicationDbContext context,
            ILogger<TrafficAnalysisController> logger,
            IFortiGateLogParserService logParser,
            ITicketNumberGenerator ticketNumberGenerator,
            IPolicyRecommendationService recommendationService,
            IAIRecommendationService aiService)
        {
            _context = context;
            _logger = logger;
            _logParser = logParser;
            _ticketNumberGenerator = ticketNumberGenerator;
            _recommendationService = recommendationService;
            _aiService = aiService;
        }

        // GET: TrafficAnalysis
        public async Task<IActionResult> Index()
        {
            var currentUserUPN = User.Identity?.Name;
            
            // Optimized query - don't load logs and recommendations, just get counts
            var analyses = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .Where(t => User.IsInRole("Admins") || t.CreatedByUPN == currentUserUPN)
                .Select(t => new
                {
                    t.TrafficAnalysisID,
                    t.TicketNumber,
                    t.Summary,
                    t.Description,
                    t.Status,
                    t.CreatedByUPN,
                    t.CreatedDate,
                    FortiGate = new
                    {
                        t.FortiGate.FGHostname,
                        FTAService = new
                        {
                            t.FortiGate.FTAService.JobID,
                            Customer = new
                            {
                                t.FortiGate.FTAService.Customer.CustomerName
                            }
                        }
                    },
                    LogCount = _context.TrafficLogs.Count(l => l.TrafficAnalysisID == t.TrafficAnalysisID),
                    RecommendationCount = _context.TrafficAnalysisRecommendations.Count(r => r.TrafficAnalysisID == t.TrafficAnalysisID)
                })
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // Map to view models
            var viewModels = analyses.Select(a => new TrafficAnalysis
            {
                TrafficAnalysisID = a.TrafficAnalysisID,
                TicketNumber = a.TicketNumber,
                Summary = a.Summary,
                Description = a.Description,
                Status = a.Status,
                CreatedByUPN = a.CreatedByUPN,
                CreatedDate = a.CreatedDate,
                FortiGate = new FortiGate
                {
                    FGHostname = a.FortiGate.FGHostname,
                    FTAService = new FTAService
                    {
                        JobID = a.FortiGate.FTAService.JobID,
                        Customer = new Customer
                        {
                            CustomerName = a.FortiGate.FTAService.Customer.CustomerName
                        }
                    }
                },
                TrafficLogs = Enumerable.Repeat(new TrafficLog(), a.LogCount).ToList(), // Fake list for count
                Recommendations = Enumerable.Repeat(new TrafficAnalysisRecommendation(), a.RecommendationCount).ToList() // Fake list for count
            }).ToList();

            return View(viewModels);
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

            // Optimized query - only load basic info, not all logs
            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .Include(t => t.Customer)
                .Include(t => t.FTAService)
                .Select(t => new
                {
                    t.TrafficAnalysisID,
                    t.TicketNumber,
                    t.Summary,
                    t.Description,
                    t.Status,
                    t.CreatedByUPN,
                    t.CreatedDate,
                    t.FGID,
                    t.FTAID,
                    t.CustomerID,
                    FortiGate = new
                    {
                        t.FortiGate.FGHostname,
                        t.FortiGate.FGHost,
                        FTAService = new
                        {
                            t.FortiGate.FTAService.JobID,
                            Customer = new
                            {
                                t.FortiGate.FTAService.Customer.CustomerName
                            }
                        }
                    },
                    LogCount = _context.TrafficLogs.Count(l => l.TrafficAnalysisID == t.TrafficAnalysisID),
                    RecommendationCount = _context.TrafficAnalysisRecommendations.Count(r => r.TrafficAnalysisID == t.TrafficAnalysisID)
                })
                .FirstOrDefaultAsync(m => m.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            // Get the full entity for authorization check
            var fullAnalysis = await _context.TrafficAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TrafficAnalysisID == id);

            // Authorization check
            if (!User.IsInRole("Admins") && fullAnalysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            // Map to view model
            var viewModel = new TrafficAnalysis
            {
                TrafficAnalysisID = analysis.TrafficAnalysisID,
                TicketNumber = analysis.TicketNumber,
                Summary = analysis.Summary,
                Description = analysis.Description,
                Status = analysis.Status,
                CreatedByUPN = analysis.CreatedByUPN,
                CreatedDate = analysis.CreatedDate,
                FGID = analysis.FGID,
                FortiGate = new FortiGate
                {
                    FGHostname = analysis.FortiGate.FGHostname,
                    FGHost = analysis.FortiGate.FGHost,
                    FTAService = new FTAService
                    {
                        JobID = analysis.FortiGate.FTAService.JobID,
                        Customer = new Customer
                        {
                            CustomerName = analysis.FortiGate.FTAService.Customer.CustomerName
                        }
                    }
                },
                TrafficLogs = new List<TrafficLog>(), // Empty list - logs loaded via AJAX
                Recommendations = new List<TrafficAnalysisRecommendation>() // Empty list
            };

            // Pass counts via ViewBag
            ViewBag.LogCount = analysis.LogCount;
            ViewBag.RecommendationCount = analysis.RecommendationCount;

            return View(viewModel);
        }

        // GET: TrafficAnalysis/Analyze/5
        public async Task<IActionResult> Analyze(Guid? id)
        {
            if (id == null)
                return NotFound();

            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .AsNoTracking()
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

        // GET: TrafficAnalysis/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var analysis = await _context.TrafficAnalyses
                .Include(t => t.FortiGate)
                    .ThenInclude(f => f.FTAService)
                        .ThenInclude(s => s.Customer)
                .FirstOrDefaultAsync(m => m.TrafficAnalysisID == id);

            if (analysis == null)
                return NotFound();

            // Authorization check - only creator or admins can edit
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Forbid();
            }

            var viewModel = new EditTrafficAnalysisViewModel
            {
                TrafficAnalysisID = analysis.TrafficAnalysisID,
                TicketNumber = analysis.TicketNumber,
                Summary = analysis.Summary,
                Description = analysis.Description,
                CustomerName = analysis.FortiGate?.FTAService?.Customer?.CustomerName ?? "N/A",
                ServiceJobID = analysis.FortiGate?.FTAService?.JobID ?? "N/A",
                FortiGateHostname = analysis.FortiGate?.FGHostname ?? "N/A",
                Status = analysis.Status,
                CreatedByUPN = analysis.CreatedByUPN,
                CreatedDate = analysis.CreatedDate
            };

            return View(viewModel);
        }

        // POST: TrafficAnalysis/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditTrafficAnalysisViewModel viewModel)
        {
            _logger.LogInformation("Edit POST called for ticket {TicketID}", id);
            _logger.LogInformation("ViewModel - TrafficAnalysisID: {VmId}, Summary: {Summary}", 
                viewModel.TrafficAnalysisID, viewModel.Summary);
            
            if (id != viewModel.TrafficAnalysisID)
            {
                _logger.LogWarning("ID mismatch: route={RouteId}, model={ModelId}", id, viewModel.TrafficAnalysisID);
                return NotFound();
            }

            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
            {
                _logger.LogWarning("Ticket not found: {TicketID}", id);
                return NotFound();
            }

            // Authorization check - only creator or admins can edit
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                _logger.LogWarning("Unauthorized edit attempt by {User}", User.Identity?.Name);
                return Forbid();
            }

            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState validation failed");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("Validation error - Key: {Key}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
                
                // Reload read-only fields
                var temp = await _context.TrafficAnalyses
                    .Include(t => t.FortiGate)
                        .ThenInclude(f => f.FTAService)
                            .ThenInclude(s => s.Customer)
                    .FirstOrDefaultAsync(m => m.TrafficAnalysisID == id);

                viewModel.CustomerName = temp?.FortiGate?.FTAService?.Customer?.CustomerName ?? "N/A";
                viewModel.ServiceJobID = temp?.FortiGate?.FTAService?.JobID ?? "N/A";
                viewModel.FortiGateHostname = temp?.FortiGate?.FGHostname ?? "N/A";
                viewModel.Status = temp?.Status ?? "Unknown";
                viewModel.CreatedByUPN = temp?.CreatedByUPN ?? "Unknown";
                viewModel.CreatedDate = temp?.CreatedDate ?? DateTime.UtcNow;

                return View(viewModel);
            }

            // Update only editable fields
            analysis.Summary = viewModel.Summary;
            analysis.Description = viewModel.Description;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket updated successfully!";
                _logger.LogInformation("Traffic Analysis ticket {TicketNumber} updated by {User}",
                    analysis.TicketNumber, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket {TicketNumber}", analysis.TicketNumber);
                TempData["ErrorMessage"] = "Error updating ticket. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = analysis.TrafficAnalysisID });
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

        // GET: TrafficAnalysis/ViewConfig/5
        public async Task<IActionResult> ViewConfig(Guid? id)
        {
            if (id == null)
                return NotFound();

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

            if (string.IsNullOrEmpty(analysis.FortiGate?.ConfigFile))
            {
                TempData["ErrorMessage"] = "No configuration file uploaded for this FortiGate.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Return as plain text for viewing
            return Content(analysis.FortiGate.ConfigFile, "text/plain");
        }

        // GET: TrafficAnalysis/ConfigDiagnostics/5
        [HttpGet]
        public async Task<IActionResult> ConfigDiagnostics(Guid? id)
        {
            if (id == null)
                return NotFound();

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

            var fortiGate = analysis.FortiGate;
            
            if (fortiGate == null)
            {
                return Json(new { success = false, message = "FortiGate not found" });
            }

            // Get diagnostics
            var decompressedConfig = fortiGate.ConfigFile;
            var decompressedLines = decompressedConfig?.Split('\n') ?? Array.Empty<string>();
            
            // Calculate checksum of decompressed data
            string currentChecksum = null;
            if (decompressedConfig != null)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(decompressedConfig);
                    var hash = sha256.ComputeHash(bytes);
                    currentChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            
            var diagnostics = new
            {
                success = true,
                fortiGateHostname = fortiGate.FGHostname,
                configUploadedDate = fortiGate.ConfigUploadedDate,
                
                // Compressed data info
                hasCompressedData = fortiGate.ConfigFileCompressed != null,
                compressedSizeBytes = fortiGate.ConfigFileCompressed?.Length ?? 0,
                compressedSizeKB = (fortiGate.ConfigFileCompressed?.Length ?? 0) / 1024.0,
                compressedFirstBytes = fortiGate.ConfigFileCompressed != null && fortiGate.ConfigFileCompressed.Length > 10
                    ? BitConverter.ToString(fortiGate.ConfigFileCompressed.Take(10).ToArray()).Replace("-", " ")
                    : null,
                isValidGzip = fortiGate.ConfigFileCompressed != null && fortiGate.ConfigFileCompressed.Length >= 2
                    ? fortiGate.ConfigFileCompressed[0] == 0x1F && fortiGate.ConfigFileCompressed[1] == 0x8B
                    : false,
                
                // Decompressed data info
                decompressedSuccessfully = decompressedConfig != null,
                decompressedSizeBytes = decompressedConfig?.Length ?? 0,
                decompressedSizeKB = (decompressedConfig?.Length ?? 0) / 1024.0,
                lineCount = decompressedLines.Length,
                compressionRatio = decompressedConfig != null && fortiGate.ConfigFileCompressed != null
                    ? (fortiGate.ConfigFileCompressed.Length * 100.0 / decompressedConfig.Length)
                    : 0,
                
                // Content validation
                hasConfigKeyword = decompressedConfig?.Contains("config") ?? false,
                hasEndKeyword = decompressedConfig?.Contains("end") ?? false,
                hasSystemSection = decompressedConfig?.Contains("config system") ?? false,
                hasFirewallSection = decompressedConfig?.Contains("config firewall") ?? false,
                hasSdwanSection = decompressedConfig?.Contains("config system sdwan") ?? false,
                
                // Sample content
                firstLines = decompressedLines.Take(10).ToList(),
                lastLines = decompressedLines.TakeLast(10).ToList(),
                
                // Integrity
                checksum = currentChecksum?.Substring(0, 16) + "...",
                
                // Truncation detection
                endsWithEnd = decompressedLines.LastOrDefault()?.Trim() == "end",
                hasCompleteStructure = decompressedConfig?.Count(c => c == '{') == decompressedConfig?.Count(c => c == '}'),
                
                // Size comparison
                expectedMinSize = 50000, // Configs are typically > 50KB
                isSuspiciouslySmall = (decompressedConfig?.Length ?? 0) < 50000,
                
                // Detailed stats
                configSections = CountConfigSections(decompressedConfig),
                
                warning = (decompressedConfig?.Length ?? 0) < 50000 
                    ? "?? Config file seems suspiciously small. May be truncated."
                    : decompressedLines.LastOrDefault()?.Trim() != "end"
                    ? "?? Config doesn't end with 'end' keyword. May be truncated."
                    : null
            };

            return Json(diagnostics);
        }
        
        private Dictionary<string, int> CountConfigSections(string config)
        {
            if (string.IsNullOrEmpty(config))
                return new Dictionary<string, int>();
                
            return new Dictionary<string, int>
            {
                ["interfaces"] = System.Text.RegularExpressions.Regex.Matches(config, @"config system interface").Count,
                ["addresses"] = System.Text.RegularExpressions.Regex.Matches(config, @"config firewall address\b").Count,
                ["policies"] = System.Text.RegularExpressions.Regex.Matches(config, @"config firewall policy").Count,
                ["services"] = System.Text.RegularExpressions.Regex.Matches(config, @"config firewall service").Count,
                ["sdwan"] = System.Text.RegularExpressions.Regex.Matches(config, @"config system sdwan").Count,
                ["routes"] = System.Text.RegularExpressions.Regex.Matches(config, @"config router static").Count,
                ["vpn"] = System.Text.RegularExpressions.Regex.Matches(config, @"config vpn").Count,
            };
        }

        // POST: TrafficAnalysis/UploadConfigFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadConfigFile(Guid id, IFormFile configFile)
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

            if (configFile == null || configFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a configuration file to upload.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".conf", ".cfg", ".txt" };
            var fileExtension = Path.GetExtension(configFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only .conf, .cfg, and .txt files are allowed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate file size (max 10MB)
            if (configFile.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size exceeds the maximum limit of 10MB.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                string configContent;
                
                using (var reader = new StreamReader(configFile.OpenReadStream(), System.Text.Encoding.UTF8))
                {
                    configContent = await reader.ReadToEndAsync();
                }

                // Validate file is not empty
                if (string.IsNullOrWhiteSpace(configContent))
                {
                    TempData["ErrorMessage"] = "The configuration file is empty.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Normalize line endings (handles Windows/Linux/Mac formats)
                configContent = configContent.Replace("\r\n", "\n").Replace("\r", "\n");

                // Validate it looks like a FortiGate config
                if (!configContent.Contains("config") || !configContent.Contains("end"))
                {
                    TempData["ErrorMessage"] = "This doesn't appear to be a valid FortiGate configuration file.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Calculate checksum for integrity verification
                var originalLength = configContent.Length;
                var originalLines = configContent.Split('\n').Length;
                var originalChecksum = CalculateSHA256(configContent);

                // Get file size info for logging
                var sizeKB = configContent.Length / 1024.0;

                _logger.LogInformation(
                    "Config file parsed: {Lines} lines, {Size:F2} KB, SHA256: {Checksum}",
                    originalLines, sizeKB, originalChecksum.Substring(0, 16) + "...");

                // Check if file is too large (> 5MB might cause issues)
                if (configContent.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning(
                        "Large config file detected: {Size:F2} MB. This may affect AI performance.",
                        configContent.Length / (1024.0 * 1024.0));
                    
                    TempData["WarningMessage"] = 
                        $"Configuration file is large ({sizeKB / 1024:F2} MB). " +
                        "AI will use a subset of the configuration for analysis.";
                }

                // Update FortiGate entity
                var fortiGate = await _context.FortiGates.FindAsync(analysis.FGID);
                if (fortiGate != null)
                {
                    // Save config (will be automatically compressed by the property setter)
                    fortiGate.ConfigFile = configContent;
                    fortiGate.ConfigUploadedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Verify integrity by reading back
                    var savedConfig = fortiGate.ConfigFile;
                    var savedChecksum = CalculateSHA256(savedConfig ?? "");
                    var integrityCheck = originalChecksum == savedChecksum;

                    if (!integrityCheck)
                    {
                        _logger.LogError(
                            "Config integrity check FAILED! Original: {Original}, Saved: {Saved}",
                            originalChecksum.Substring(0, 16), savedChecksum.Substring(0, 16));
                        
                        TempData["ErrorMessage"] = 
                            "Configuration file upload failed integrity check. Please try again.";
                        
                        // Rollback
                        fortiGate.ConfigFile = null;
                        fortiGate.ConfigUploadedDate = null;
                        await _context.SaveChangesAsync();
                        
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    _logger.LogInformation(
                        "Config integrity verified: {Lines} lines preserved, Checksum match: {Match}",
                        originalLines, integrityCheck);

                    TempData["SuccessMessage"] = 
                        $"Configuration file uploaded successfully! " +
                        $"({originalLines:N0} lines, {sizeKB:F2} KB) " +
                        $"? Integrity verified";
                    
                    _logger.LogInformation(
                        "Config file uploaded for FortiGate {FGID} by {User}. " +
                        "Original: {OrigSize} bytes, Compressed: {CompSize} bytes, Ratio: {Ratio:F1}%",
                        fortiGate.FGID, User.Identity?.Name, 
                        originalLength,
                        fortiGate.ConfigFileCompressed?.Length ?? 0,
                        (fortiGate.ConfigFileCompressed?.Length ?? 0) * 100.0 / originalLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading config file for ticket {TicketID}", analysis.TrafficAnalysisID);
                TempData["ErrorMessage"] = $"Error processing configuration file: {ex.Message}";
            }

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
        public async Task<IActionResult> GetLogs(
            Guid id, 
            string? search = null, 
            string? filterAction = null,
            string? srcIp = null, 
            string? dstIp = null, 
            string? srcPort = null,
            string? dstPort = null,
            string? srcIntf = null,
            string? dstIntf = null,
            string? proto = null,
            string? service = null,
            string? policyId = null,
            string? policyName = null,
            string? logId = null,
            string? sessionId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int page = 1,
            int pageSize = 100)
        {
            _logger.LogInformation("GetLogs called for ticket {TicketID} - Page {Page}, PageSize {PageSize}", id, page, pageSize);

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

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => 
                    (l.SrcIP != null && l.SrcIP.Contains(search)) ||
                    (l.DstIP != null && l.DstIP.Contains(search)) ||
                    (l.SrcPort != null && l.SrcPort.Contains(search)) ||
                    (l.DstPort != null && l.DstPort.Contains(search)) ||
                    (l.Service != null && l.Service.Contains(search)) ||
                    (l.PolicyName != null && l.PolicyName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filterAction))
                query = query.Where(l => l.Action == filterAction);

            if (!string.IsNullOrWhiteSpace(srcIp))
                query = query.Where(l => l.SrcIP != null && l.SrcIP.Contains(srcIp));

            if (!string.IsNullOrWhiteSpace(dstIp))
                query = query.Where(l => l.DstIP != null && l.DstIP.Contains(dstIp));

            if (!string.IsNullOrWhiteSpace(srcPort))
                query = query.Where(l => l.SrcPort != null && l.SrcPort.Contains(srcPort));

            if (!string.IsNullOrWhiteSpace(dstPort))
                query = query.Where(l => l.DstPort != null && l.DstPort.Contains(dstPort));

            if (!string.IsNullOrWhiteSpace(srcIntf))
                query = query.Where(l => l.SrcInt != null && l.SrcInt.Contains(srcIntf));

            if (!string.IsNullOrWhiteSpace(dstIntf))
                query = query.Where(l => l.DstInt != null && l.DstInt.Contains(dstIntf));

            if (!string.IsNullOrWhiteSpace(proto))
                query = query.Where(l => l.Proto == proto);

            if (!string.IsNullOrWhiteSpace(service))
                query = query.Where(l => l.Service != null && l.Service.Contains(service));

            if (!string.IsNullOrWhiteSpace(policyId))
                query = query.Where(l => l.PolicyId != null && l.PolicyId.Contains(policyId));

            if (!string.IsNullOrWhiteSpace(policyName))
                query = query.Where(l => l.PolicyName != null && l.PolicyName.Contains(policyName));

            if (!string.IsNullOrWhiteSpace(logId))
                query = query.Where(l => l.LogId != null && l.LogId.Contains(logId));

            if (!string.IsNullOrWhiteSpace(sessionId))
                query = query.Where(l => l.SessionId != null && l.SessionId.Contains(sessionId));

            if (dateFrom.HasValue)
                query = query.Where(l => l.LogDate >= dateFrom.Value.Date);

            if (dateTo.HasValue)
                query = query.Where(l => l.LogDate <= dateTo.Value.Date);

            var filteredCount = await query.CountAsync();

            // Apply pagination
            var logs = await query
                .OrderByDescending(l => l.LogDate)
                .ThenByDescending(l => l.LogTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            _logger.LogInformation("Returning page {Page} with {Count} logs (Total: {Total}, Filtered: {Filtered})", 
                page, logs.Count, totalCount, filteredCount);

            return Json(new
            {
                logs = logs,
                pagination = new
                {
                    page = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    filteredCount = filteredCount,
                    totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize)
                }
            });
        }

        // POST: TrafficAnalysis/GetLogsByIds
        [HttpPost]
        public async Task<IActionResult> GetLogsByIds([FromBody] GetLogsByIdsRequest request)
        {
            if (request.LogIds == null || !request.LogIds.Any())
            {
                return Json(new { success = false, message = "No log IDs provided" });
            }

            var logs = await _context.TrafficLogs
                .Where(l => request.LogIds.Contains(l.TrafficLogID) && l.TrafficAnalysisID == request.TicketId)
                .Select(l => new
                {
                    l.TrafficLogID,
                    l.LogDate,
                    l.LogTime,
                    l.SrcIP,
                    l.SrcPort,
                    l.DstIP,
                    l.DstPort,
                    l.Proto,
                    l.Action,
                    l.Service
                })
                .ToListAsync();

            return Json(new { success = true, logs = logs });
        }

        // POST: TrafficAnalysis/AnalyzeSelected
        [HttpPost]
        public async Task<IActionResult> AnalyzeSelected(Guid id, [FromBody] List<Guid> logIds)
        {
            _logger.LogInformation("AnalyzeSelected called for ticket {TicketID} with {Count} logs", id, logIds?.Count ?? 0);

            if (logIds == null || !logIds.Any())
            {
                return Json(new { success = false, message = "No logs selected for analysis" });
            }

            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
            {
                return Json(new { success = false, message = "Ticket not found" });
            }

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                // Get selected logs from database
                var selectedLogs = await _context.TrafficLogs
                    .Where(l => logIds.Contains(l.TrafficLogID) && l.TrafficAnalysisID == id)
                    .ToListAsync();

                if (!selectedLogs.Any())
                {
                    return Json(new { success = false, message = "Selected logs not found" });
                }

                _logger.LogInformation("Analyzing {Count} logs for ticket {TicketID}", selectedLogs.Count, id);

                // Generate recommendations
                var recommendations = _recommendationService.AnalyzeLogs(
                    selectedLogs,
                    analysis.TrafficAnalysisID,
                    User.Identity?.Name ?? "Unknown");

                if (recommendations.Any())
                {
                    await _context.TrafficAnalysisRecommendations.AddRangeAsync(recommendations);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created {Count} recommendations for ticket {TicketID}",
                        recommendations.Count, id);

                    return Json(new
                    {
                        success = true,
                        message = $"Successfully generated {recommendations.Count} recommendation(s)",
                        count = recommendations.Count
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "No recommendations could be generated from the selected logs"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing logs for ticket {TicketID}", id);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: TrafficAnalysis/AskAI
        [HttpPost]
        public async Task<IActionResult> AskAI(Guid id, [FromBody] AIQuestionRequest request)
        {
            _logger.LogInformation("AskAI called for ticket {TicketID}: {Question}",
                id, request?.Question);

            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return Json(new { success = false, message = "Question cannot be empty" });
            }

            if (request.SelectedLogIds == null || !request.SelectedLogIds.Any())
            {
                return Json(new { success = false, message = "Please select at least one log entry" });
            }

            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
            {
                return Json(new { success = false, message = "Ticket not found" });
            }

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                // Get selected logs
                var selectedLogs = await _context.TrafficLogs
                    .Where(l => request.SelectedLogIds.Contains(l.TrafficLogID) &&
                                l.TrafficAnalysisID == id)
                    .ToListAsync();

                if (!selectedLogs.Any())
                {
                    return Json(new { success = false, message = "Selected logs not found" });
                }

                _logger.LogInformation("Processing AI question for {Count} logs", selectedLogs.Count);

                // Call AI service
                var aiResponse = await _aiService.AskQuestionAsync(
                    id,
                    request.Question,
                    selectedLogs,
                    User.Identity?.Name ?? "Unknown");

                return Json(new
                {
                    success = true,
                    response = aiResponse,
                    logsAnalyzed = selectedLogs.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI question for ticket {TicketID}", id);
                return Json(new
                {
                    success = false,
                    message = "AI service temporarily unavailable. Please try again later."
                });
            }
        }

        // GET: TrafficAnalysis/GetConversationHistory/5
        [HttpGet]
        public async Task<IActionResult> GetConversationHistory(Guid id)
        {
            var analysis = await _context.TrafficAnalyses.FindAsync(id);
            if (analysis == null)
            {
                return Json(new { success = false, message = "Ticket not found" });
            }

            // Authorization check
            if (!User.IsInRole("Admins") && analysis.CreatedByUPN != User.Identity?.Name)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var conversations = await _aiService.GetConversationHistoryAsync(id);

                return Json(new
                {
                    success = true,
                    conversations = conversations.Select(c => new
                    {
                        c.ConversationID,
                        c.UserQuestion,
                        c.AIResponse,
                        c.CreatedByUPN,
                        c.CreatedDate,
                        c.TokensUsed,
                        c.ResponseTimeMs
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation history for ticket {TicketID}", id);
                return Json(new { success = false, message = "Error loading conversation history" });
            }
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

        private string CalculateSHA256(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    // Request model for AI questions
    public class AIQuestionRequest
    {
        public string Question { get; set; } = string.Empty;
        public List<Guid> SelectedLogIds { get; set; } = new List<Guid>();
    }

    // Request model for GetLogsByIds
    public class GetLogsByIdsRequest
    {
        public Guid TicketId { get; set; }
        public List<Guid> LogIds { get; set; } = new List<Guid>();
    }
}
