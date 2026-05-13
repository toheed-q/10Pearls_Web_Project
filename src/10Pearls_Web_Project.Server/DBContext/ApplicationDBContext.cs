using _10Pearls_Web_Project.Server.Enums;
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tasks>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(t => t.Description)
                      .HasMaxLength(2000);

                entity.Property(t => t.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(t => t.Priority)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(t => t.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // FK: Tasks → AspNetUsers (restrict so deleting user doesn't cascade-delete tasks silently)
                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-update UpdatedAt on every save
            var modified = ChangeTracker.Entries<Tasks>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;

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
