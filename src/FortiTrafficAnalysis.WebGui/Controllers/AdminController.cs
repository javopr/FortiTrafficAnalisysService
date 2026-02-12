using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Services.Authorization;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _context.AppUsers.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalServices = await _context.FTAServices.CountAsync(),
                TotalFortiGates = await _context.FortiGates.CountAsync(),
                TotalLogs = await _context.TrafficLogs.CountAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}
