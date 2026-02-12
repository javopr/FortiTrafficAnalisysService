using FortiTrafficAnalysis.Domain.Entities;
using System.Threading.Tasks;

namespace FortiTrafficAnalysis.Services.Authorization
{
    /// <summary>
    /// Service for handling user authorization and role management
    /// </summary>
    public interface IUserAuthorizationService
    {
        Task<AppUser?> GetUserByUPNAsync(string upn);
        Task<bool> IsUserInRoleAsync(string upn, string roleName);
        Task<string?> GetUserRoleAsync(string upn);
    }
}
