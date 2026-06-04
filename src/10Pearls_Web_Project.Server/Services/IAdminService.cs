using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public interface IAdminService
    {
        Task<List<UserSummaryDto>> GetAllUsersAsync();
        Task<UserDetailsDto?> GetUserByIdAsync(string userId);
        Task<(bool Success, string? Error)> UpdateUserRoleAsync(string adminId, string userId, string newRole);
    }
}
