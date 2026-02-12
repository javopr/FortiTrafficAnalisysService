using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FortiTrafficAnalysis.Services.Authorization
{
    /// <summary>
    /// Implementation of user authorization service
    /// </summary>
    public class UserAuthorizationService : IUserAuthorizationService
    {
        private readonly ApplicationDbContext _context;

        public UserAuthorizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetUserByUPNAsync(string upn)
        {
            return await _context.AppUsers
                .Include(u => u.AppGroup)
                .FirstOrDefaultAsync(u => u.UserUPN.ToLower() == upn.ToLower());
        }

        public async Task<bool> IsUserInRoleAsync(string upn, string roleName)
        {
            var user = await GetUserByUPNAsync(upn);
            return user?.AppGroup?.AppGroupName?.Equals(roleName, System.StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task<string?> GetUserRoleAsync(string upn)
        {
            var user = await GetUserByUPNAsync(upn);
            return user?.AppGroup?.AppGroupName;
        }
    }
}
