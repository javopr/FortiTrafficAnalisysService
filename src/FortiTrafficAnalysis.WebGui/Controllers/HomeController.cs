using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.WebGui.Models;

namespace FortiTrafficAnalysis.WebGui.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var currentUserUPN = User.Identity?.Name;
        
        // Get user's tickets (or all if admin)
        var ticketsQuery = _context.TrafficAnalyses.AsQueryable();
        
        if (!User.IsInRole("Admins"))
        {
            ticketsQuery = ticketsQuery.Where(t => t.CreatedByUPN == currentUserUPN);
        }
        
        // Calculate statistics
        var totalLogs = await _context.TrafficLogs
            .Where(l => ticketsQuery.Any(t => t.TrafficAnalysisID == l.TrafficAnalysisID))
            .CountAsync();
        
        var allowedLogs = await _context.TrafficLogs
            .Where(l => ticketsQuery.Any(t => t.TrafficAnalysisID == l.TrafficAnalysisID) && 
                       l.Action == "accept")
            .CountAsync();
        
        var deniedLogs = await _context.TrafficLogs
            .Where(l => ticketsQuery.Any(t => t.TrafficAnalysisID == l.TrafficAnalysisID) && 
                       (l.Action == "deny" || l.Action.Contains("rst")))
            .CountAsync();
        
        var totalRecommendations = await _context.TrafficAnalysisRecommendations
            .Where(r => ticketsQuery.Any(t => t.TrafficAnalysisID == r.TrafficAnalysisID))
            .CountAsync();
        
        ViewBag.TotalLogs = totalLogs;
        ViewBag.AllowedLogs = allowedLogs;
        ViewBag.DeniedLogs = deniedLogs;
        ViewBag.TotalRecommendations = totalRecommendations;
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
