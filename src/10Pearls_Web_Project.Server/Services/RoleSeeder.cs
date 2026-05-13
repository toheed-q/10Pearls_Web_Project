using Microsoft.AspNetCore.Identity;

namespace _10Pearls_Web_Project.Server.Services
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roles = ["Admin", "User"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Role '{Role}' created", role);
                }
            }
        }
    }
}
