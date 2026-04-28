using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error)> RegisterAsync(RegisterDTO dto);
        Task<(bool Success, AuthResponseDTO? Data, string? Error)> LoginAsync(LoginDTO dto);
    }
}
