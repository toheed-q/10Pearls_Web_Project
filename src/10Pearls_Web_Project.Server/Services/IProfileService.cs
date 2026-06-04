using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public interface IProfileService
    {
        Task<UserProfileDto?> GetProfileAsync(string userId);
        Task<(bool Success, string? Error)> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    }
}
