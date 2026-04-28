using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10Pearls_Web_Project.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager, ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("register")]
        [Consumes("application/json")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            _logger.LogInformation("Register request received for {Email}", dto.Email);

            var (success, error) = await _authService.RegisterAsync(dto);

            if (!success)
            {
                _logger.LogWarning("Registration failed for {Email}: {Error}", dto.Email, error);
                return BadRequest(new { message = error });
            }

            _logger.LogInformation("User registered successfully: {Email}", dto.Email);
            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);

            var (success, data, error) = await _authService.LoginAsync(dto);

            if (!success)
            {
                _logger.LogWarning("Login failed for {Email}: {Error}", dto.Email, error);
                return Unauthorized(new { message = error });
            }

            _logger.LogInformation("Login successful for {Email}", dto.Email);
            return Ok(data);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            _logger.LogInformation("Profile requested for UserId {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile not found for UserId {UserId}", userId);
                return NotFound("User not found");
            }

            return Ok(new AuthResponseDTO
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName ?? string.Empty
            });
        }
    }
}
