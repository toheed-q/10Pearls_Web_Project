using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10Pearls_Web_Project.Server.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger       = logger;
        }

        private string? AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET /api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        // GET /api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            return user == null
                ? NotFound(new { message = "User not found" })
                : Ok(user);
        }

        // PUT /api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest request)
        {
            var adminId = AdminId;
            if (adminId == null) return Unauthorized();

            _logger.LogInformation(
                "Admin {AdminId} requested role change for User {UserId} to {Role}",
                adminId, id, request.Role);

            var (success, error) = await _adminService.UpdateUserRoleAsync(adminId, id, request.Role);

            return success
                ? Ok(new { message = $"User role updated to {request.Role}" })
                : BadRequest(new { message = error });
        }
    }
}
