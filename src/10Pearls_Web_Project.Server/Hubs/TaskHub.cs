using _10Pearls_Web_Project.Server.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace _10Pearls_Web_Project.Server.Hubs
{
    [Authorize]
    public class TaskHub : Hub
    {
        private readonly ILogger<TaskHub> _logger;

        public TaskHub(ILogger<TaskHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var role   = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("SignalR connection rejected — no userId in token: {ConnectionId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            // Every authenticated user joins their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, HubEvents.UserGroup(userId));

            // Admins additionally join the admin broadcast group
            if (role == "Admin")
                await Groups.AddToGroupAsync(Context.ConnectionId, HubEvents.AdminGroup);

            _logger.LogInformation(
                "SignalR connected — ConnectionId: {ConnectionId} | UserId: {UserId} | Role: {Role}",
                Context.ConnectionId, userId, role ?? "User");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "SignalR disconnected — ConnectionId: {ConnectionId} | UserId: {UserId}",
                Context.ConnectionId, userId ?? "unknown");

            await base.OnDisconnectedAsync(exception);
        }
    }
}
