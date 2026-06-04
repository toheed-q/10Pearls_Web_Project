using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Hubs;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace _10Pearls_Web_Project.Server.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDBContext _db;
        private readonly IHubContext<TaskHub> _hub;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            ApplicationDBContext db,
            IHubContext<TaskHub> hub,
            ILogger<AdminService> logger)
        {
            _userManager = userManager;
            _db          = db;
            _hub         = hub;
            _logger      = logger;
        }

        public async Task<List<UserSummaryDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            var taskCounts = await _db.Tasks
                .GroupBy(t => t.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var result = new List<UserSummaryDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(MapToSummary(user, roles, taskCounts.GetValueOrDefault(user.Id, 0)));
            }

            _logger.LogInformation("Admin retrieved {Count} users", result.Count);
            return result;
        }

        public async Task<UserDetailsDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles      = await _userManager.GetRolesAsync(user);
            var taskCount  = await _db.Tasks.CountAsync(t => t.UserId == userId);
            var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;

            _logger.LogInformation("Admin retrieved details for User {UserId}", userId);

            return new UserDetailsDto
            {
                Id        = user.Id,
                FullName  = user.FullName ?? string.Empty,
                Email     = user.Email    ?? string.Empty,
                UserName  = user.UserName,
                Role      = primaryRole,
                TaskCount = taskCount,
                CreatedAt = DateTime.UtcNow   // Identity doesn't store CreatedAt by default
            };
        }

        public async Task<(bool Success, string? Error)> UpdateUserRoleAsync(
            string adminId, string userId, string newRole)
        {
            if (newRole != Roles.Admin && newRole != Roles.User)
                return (false, $"Invalid role '{newRole}'. Must be '{Roles.Admin}' or '{Roles.User}'.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, $"User '{userId}' not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRole = currentRoles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;

            if (oldRole == newRole)
                return (false, $"User is already a {newRole}.");

            // Guard: cannot demote the last admin
            if (oldRole == Roles.Admin && newRole == Roles.User)
            {
                var adminCount = (await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count;
                if (adminCount <= 1)
                    return (false, "Cannot demote the last Admin.");

                _logger.LogInformation(
                    "Admin {AdminId} demoted User {UserId} from Admin to User", adminId, userId);
            }
            else
            {
                _logger.LogInformation(
                    "Admin {AdminId} promoted User {UserId} to Admin", adminId, userId);
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            var payload = new UserRoleChangedPayload
            {
                UserId  = userId,
                OldRole = oldRole,
                NewRole = newRole
            };

            await _hub.Clients.Group(HubEvents.AdminGroup)
                .SendAsync(HubEvents.UserRoleChanged, payload);

            return (true, null);
        }

        // ── Private helpers ────────────────────────────────────────────────

        private static UserSummaryDto MapToSummary(
            ApplicationUser user, IList<string> roles, int taskCount) => new()
        {
            Id        = user.Id,
            FullName  = user.FullName ?? string.Empty,
            Email     = user.Email    ?? string.Empty,
            Role      = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User,
            TaskCount = taskCount
        };
    }
}
