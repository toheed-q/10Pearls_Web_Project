using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace _10Pearls_Web_Project.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JWTService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JWTService jwtService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterDTO dto)
        {
            _logger.LogInformation("Attempting to register user with email {Email}", dto.Email);

            if (await _userManager.FindByEmailAsync(dto.Email) != null)
            {
                _logger.LogWarning("Registration blocked — email already exists: {Email}", dto.Email);
                return (false, "User already exists");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User creation failed for {Email}: {Errors}", dto.Email, errors);
                return (false, errors);
            }

            // Every new user gets the User role by default
            await _userManager.AddToRoleAsync(user, Roles.User);

            _logger.LogInformation("User {UserId} registered with role 'User'", user.Id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> PromoteToAdminAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return (false, $"No user found with email '{email}'");

            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                return (false, "User is already an Admin");

            await _userManager.RemoveFromRoleAsync(user, Roles.User);
            await _userManager.AddToRoleAsync(user, Roles.Admin);

            _logger.LogInformation("User {UserId} promoted to Admin", user.Id);
            return (true, null);
        }

        public async Task<(bool Success, AuthResponseDTO? Data, string? Error)> LoginAsync(LoginDTO dto)
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed — user not found: {Email}", dto.Email);
                return (false, null, "Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed — invalid password for {Email}", dto.Email);
                return (false, null, "Invalid credentials");
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Primary role: Admin takes precedence, fallback to User
            var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;

            var token = _jwtService.GenerateToken(user, roles);

            _logger.LogInformation("User {UserId} logged in with role {Role}", user.Id, primaryRole);

            return (true, new AuthResponseDTO
            {
                Token = token,
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName ?? string.Empty,
                Role = primaryRole
            }, null);
        }
    }
}
