using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace _10Pearls_Web_Project.Server.Services
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config      = services.GetRequiredService<IConfiguration>();
            var logger      = services.GetRequiredService<ILogger<Program>>();

            // ── 1. Seed roles ────────────────────────────────────────────
            string[] roles = [Roles.Admin, Roles.User];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Role '{Role}' created", role);
                }
            }

            // ── 2. Seed default admin user ───────────────────────────────
            // Credentials come from config so they are never hardcoded in source
            var adminEmail    = config["Seed:AdminEmail"]    ?? "admin@system.com";
            var adminPassword = config["Seed:AdminPassword"] ?? "Admin@123";
            var adminFullName = config["Seed:AdminFullName"] ?? "System Admin";

            var existing = await userManager.FindByEmailAsync(adminEmail);

            if (existing == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName       = adminEmail,
                    Email          = adminEmail,
                    FullName       = adminFullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    logger.LogInformation("Default admin user created: {Email}", adminEmail);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogWarning("Failed to create default admin: {Errors}", errors);
                }
            }
            else if (!await userManager.IsInRoleAsync(existing, Roles.Admin))
            {
                // Existing user found but missing Admin role — fix it
                await userManager.AddToRoleAsync(existing, Roles.Admin);
                logger.LogInformation("Admin role assigned to existing user: {Email}", adminEmail);
            }
        }
    }
}
