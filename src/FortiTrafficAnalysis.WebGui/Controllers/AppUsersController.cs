using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using FortiTrafficAnalysis.WebGui.Models;
using FortiTrafficAnalysis.Services.Authorization;
using FortiTrafficAnalysis.Services.Authentication;

namespace FortiTrafficAnalysis.WebGui.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public class AppUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocalAuthenticationService _authService;
        private readonly ILogger<AppUsersController> _logger;

        public AppUsersController(
            ApplicationDbContext context,
            ILocalAuthenticationService authService,
            ILogger<AppUsersController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        // GET: AppUsers
        public async Task<IActionResult> Index()
        {
            var users = await _context.AppUsers
                .Include(u => u.AppGroup)
                .Select(u => new AppUserViewModel
                {
                    AppAccessID = u.AppAccessID,
                    UserUPN = u.UserUPN,
                    AppUserName = u.AppUserName,
                    AppUserEmail = u.AppUserEmail,
                    AppGroupID = u.AppGroupID,
                    AppGroupName = u.AppGroup.AppGroupName
                })
                .OrderBy(u => u.AppUserName)
                .ToListAsync();

            return View(users);
        }

        // GET: AppUsers/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AppGroups = new SelectList(await _context.AppGroups.ToListAsync(), "AppGroupID", "AppGroupName");
            return View();
        }

        // POST: AppUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user already exists
                if (await _context.AppUsers.AnyAsync(u => u.UserUPN.ToLower() == model.UserUPN.ToLower()))
                {
                    ModelState.AddModelError("UserUPN", "A user with this username already exists.");
                    ViewBag.AppGroups = new SelectList(await _context.AppGroups.ToListAsync(), "AppGroupID", "AppGroupName");
                    return View(model);
                }

                var user = new AppUser
                {
                    UserUPN = model.UserUPN,
                    AppUserName = model.AppUserName,
                    AppUserEmail = model.AppUserEmail,
                    AppGroupID = model.AppGroupID,
                    PasswordHash = _authService.HashPassword(model.Password)
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"User '{user.AppUserName}' created successfully!";
                _logger.LogInformation("User created: {UserUPN} by {Admin}", user.UserUPN, User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }

            ViewBag.AppGroups = new SelectList(await _context.AppGroups.ToListAsync(), "AppGroupID", "AppGroupName");
            return View(model);
        }

        // GET: AppUsers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound();

            var model = new AppUserViewModel
            {
                AppAccessID = user.AppAccessID,
                UserUPN = user.UserUPN,
                AppUserName = user.AppUserName,
                AppUserEmail = user.AppUserEmail,
                AppGroupID = user.AppGroupID
            };

            ViewBag.AppGroups = new SelectList(await _context.AppGroups.ToListAsync(), "AppGroupID", "AppGroupName", user.AppGroupID);
            return View(model);
        }

        // POST: AppUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AppUserViewModel model)
        {
            if (id != model.AppAccessID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.AppUsers.FindAsync(id);
                    if (user == null)
                        return NotFound();

                    user.UserUPN = model.UserUPN;
                    user.AppUserName = model.AppUserName;
                    user.AppUserEmail = model.AppUserEmail;
                    user.AppGroupID = model.AppGroupID;

                    // Update password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.PasswordHash = _authService.HashPassword(model.Password);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"User '{user.AppUserName}' updated successfully!";
                    _logger.LogInformation("User updated: {UserUPN} by {Admin}", user.UserUPN, User.Identity.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AppGroups = new SelectList(await _context.AppGroups.ToListAsync(), "AppGroupID", "AppGroupName", model.AppGroupID);
            return View(model);
        }

        // GET: AppUsers/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.AppUsers
                .Include(u => u.AppGroup)
                .FirstOrDefaultAsync(m => m.AppAccessID == id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: AppUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _context.AppUsers
                .Include(u => u.AppGroup)
                .FirstOrDefaultAsync(u => u.AppAccessID == id);

            if (user == null)
                return NotFound();

            // Prevent admin from deleting themselves
            var currentUserUPN = User.Identity?.Name;
            if (user.UserUPN.Equals(currentUserUPN, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deleting the last admin
            var adminGroupId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            if (user.AppGroupID == adminGroupId)
            {
                var adminCount = await _context.AppUsers.CountAsync(u => u.AppGroupID == adminGroupId);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the last administrator account.";
                    return RedirectToAction(nameof(Index));
                }
            }

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.AppUserName}' deleted successfully!";
            _logger.LogInformation("User deleted: {UserUPN} by {Admin}", user.UserUPN, User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(Guid id)
        {
            return _context.AppUsers.Any(e => e.AppAccessID == id);
        }
    }
}
