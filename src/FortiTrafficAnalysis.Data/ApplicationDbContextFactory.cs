using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FortiTrafficAnalysis.Data
{
    /// <summary>
    /// Factory for creating DbContext at design time (for migrations)
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use Azure SQL Database connection string for migrations
            optionsBuilder.UseSqlServer(
                "Server=intechsql.database.windows.net;Database=fgtas;User Id=sqladmin;Password=C5hARST7Ak9pQpQB;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true",
                b => b.MigrationsAssembly("FortiTrafficAnalysis.Data"));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
