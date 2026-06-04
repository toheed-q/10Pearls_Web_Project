using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _10Pearls_Web_Project.Server.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDBContext _db;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            ApplicationDBContext db,
            ILogger<ProfileService> logger)
        {
            _userManager = userManager;
            _db          = db;
            _logger      = logger;
        }

        public async Task<UserProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var role  = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;

            var stats = await _db.Tasks
                .Where(t => t.UserId == userId)
                .GroupBy(_ => 1)
                .Select(g => new ProfileTaskStatsDto
                {
                    Total      = g.Count(),
                    Completed  = g.Count(t => t.Status == AppTaskStatus.Completed),
                    Pending    = g.Count(t => t.Status == AppTaskStatus.Pending),
                    InProgress = g.Count(t => t.Status == AppTaskStatus.InProgress)
                })
                .FirstOrDefaultAsync() ?? new ProfileTaskStatsDto();

            _logger.LogInformation("User {UserId} fetched profile", userId);

            return new UserProfileDto
            {
                Id        = user.Id,
                FullName  = user.FullName ?? string.Empty,
                Email     = user.Email    ?? string.Empty,
                Role      = role,
                TaskStats = stats
            };
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(
            string userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName.Trim();
                var nameResult = await _userManager.UpdateAsync(user);
                if (!nameResult.Succeeded)
                    return (false, string.Join(", ", nameResult.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentPassword) &&
                !string.IsNullOrWhiteSpace(request.NewPassword))
            {
                var pwResult = await _userManager.ChangePasswordAsync(
                    user, request.CurrentPassword, request.NewPassword);

                if (!pwResult.Succeeded)
                    return (false, string.Join(", ", pwResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("User {UserId} updated profile", userId);
            return (true, null);
        }
    }
}
