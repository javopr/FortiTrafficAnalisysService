using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FortiTrafficAnalysis.Services.Authentication
{
    /// <summary>
    /// Service for local authentication
    /// </summary>
    public interface ILocalAuthenticationService
    {
        Task<AppUser?> AuthenticateAsync(string username, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Implementation of local authentication service
    /// </summary>
    public class LocalAuthenticationService : ILocalAuthenticationService
    {
        private readonly ApplicationDbContext _context;

        public LocalAuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.AppUsers
                .Include(u => u.AppGroup)
                .FirstOrDefaultAsync(u => u.UserUPN.ToLower() == username.ToLower());

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return null;

            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hash == hashOfInput;
        }
    }
}
