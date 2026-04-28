using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _10Pearls_Web_Project.Server.DBContext
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ILogger<ApplicationDBContext> _logger;

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options, ILogger<ApplicationDBContext> logger)
            : base(options)
        {
            _logger = logger;
        }

        public DbSet<Tasks> Tasks { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error during SaveChangesAsync");
                throw;
            }
        }
    }
}
