using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Hubs;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static _10Pearls_Web_Project.Server.Enums.HubEvents;

namespace _10Pearls_Web_Project.Server.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<TaskService> _logger;
        private readonly IHubContext<TaskHub> _hub;

        public TaskService(
            ApplicationDBContext db,
            ILogger<TaskService> logger,
            IHubContext<TaskHub> hub)
        {
            _db    = db;
            _logger = logger;
            _hub   = hub;
        }

        public async Task<TaskResponseDTO> CreateTaskAsync(string userId, CreateTaskDTO dto)
        {
            var task = new Tasks
            {
                Id          = Guid.NewGuid(),
                Title       = dto.Title,
                Description = dto.Description,
                Status      = dto.Status,
                Priority    = dto.Priority,
                DueDate     = dto.DueDate,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow,
                UserId      = userId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            // Reload with navigation property so OwnerName is populated in the response
            await _db.Entry(task).Reference(t => t.User).LoadAsync();

            _logger.LogInformation("Task {TaskId} created for User {UserId}", task.Id, userId);

            var result = MapToDTO(task);

            // Notify the task owner + all admins — no other users receive this
            await SendToUserAndAdmins(userId, TaskCreated, result);

            return result;
        }

        public async Task<TaskPagedResponseDTO> GetTasksAsync(
            string userId, bool isAdmin,
            int page, int pageSize,
            string? status, string? search, string sortOrder)
        {
            var query = isAdmin
                ? _db.Tasks.Include(t => t.User).AsQueryable()
                : _db.Tasks.Include(t => t.User).Where(t => t.UserId == userId);

            // Server-side status filter
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<AppTaskStatus>(status, out var parsedStatus))
                query = query.Where(t => t.Status == parsedStatus);

            // Server-side keyword search across title and description
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    (t.Description != null && t.Description.Contains(search)));

            // Sort by due date
            query = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(t => t.DueDate)
                : query.OrderByDescending(t => t.DueDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapToDTO(t))
                .ToListAsync();

            _logger.LogInformation(
                "Page {Page}/{TotalPages} ({Count} items) for User {UserId} (isAdmin={IsAdmin})",
                page, Math.Max(1, totalPages), items.Count, userId, isAdmin);

            return new TaskPagedResponseDTO
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize,
                TotalPages = Math.Max(1, totalPages)
            };
        }

        public async Task<TaskResponseDTO?> GetTaskByIdAsync(string userId, Guid taskId, bool isAdmin)
        {
            var task = isAdmin
                ? await _db.Tasks.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == taskId)
                : await _db.Tasks.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for User {UserId}", taskId, userId);
                return null;
            }

            return MapToDTO(task);
        }

        public async Task<TaskResponseDTO?> UpdateTaskAsync(string userId, Guid taskId, UpdateTaskDTO dto, bool isAdmin)
        {
            var task = await _db.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId && (isAdmin || t.UserId == userId));

            if (task == null)
            {
                _logger.LogWarning("Update failed — Task {TaskId} not found for User {UserId}", taskId, userId);
                return null;
            }

            var previousStatus = task.Status;

            if (dto.Title       != null)  task.Title       = dto.Title;
            if (dto.Description != null)  task.Description = dto.Description;
            if (dto.DueDate.HasValue)     task.DueDate     = dto.DueDate.Value;
            if (dto.Status.HasValue)      task.Status      = dto.Status.Value;
            if (dto.Priority.HasValue)    task.Priority    = dto.Priority.Value;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} updated for User {UserId}", taskId, userId);

            var result = MapToDTO(task);

            // Status change gets its own event so clients can react differently if needed
            var eventName = (dto.Status.HasValue && dto.Status.Value != previousStatus)
                ? TaskStatusChanged
                : TaskUpdated;

            // Notify the task owner — use task.UserId (not caller's userId) so the
            // owner always receives the event even when an admin made the change
            await SendToUserAndAdmins(task.UserId, eventName, result);

            return result;
        }

        public async Task<bool> DeleteTaskAsync(string userId, Guid taskId, bool isAdmin)
        {
            var task = await _db.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId && (isAdmin || t.UserId == userId));

            if (task == null)
            {
                _logger.LogWarning("Delete failed — Task {TaskId} not found for User {UserId}", taskId, userId);
                return false;
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} deleted by User {UserId}", taskId, userId);

            // Send taskId (as string) so clients can remove it from state by id
            // Notify the task owner regardless of who deleted it
            await SendToUserAndAdmins(task.UserId, TaskDeleted, taskId.ToString());

            return true;
        }

        public async Task<TaskStatsDTO> GetStatsAsync(string userId, bool isAdmin)
        {
            var query = isAdmin
                ? _db.Tasks.AsQueryable()
                : _db.Tasks.Where(t => t.UserId == userId);

            var stats = await query
                .GroupBy(_ => 1)
                .Select(g => new TaskStatsDTO
                {
                    Total      = g.Count(),
                    Pending    = g.Count(t => t.Status == AppTaskStatus.Pending),
                    InProgress = g.Count(t => t.Status == AppTaskStatus.InProgress),
                    Completed  = g.Count(t => t.Status == AppTaskStatus.Completed)
                })
                .FirstOrDefaultAsync();

            return stats ?? new TaskStatsDTO();
        }

        // ── Private helpers ────────────────────────────────────────────────

        /// <summary>
        /// Sends a SignalR event to the task owner's personal group AND the admin group.
        /// This is the ONLY emission pattern — no Clients.All, no other groups.
        /// </summary>
        private async Task SendToUserAndAdmins(string userId, string eventName, object payload)
        {
            await _hub.Clients.Group(UserGroup(userId)).SendAsync(eventName, payload);
            await _hub.Clients.Group(AdminGroup).SendAsync(eventName, payload);
        }

        private static TaskResponseDTO MapToDTO(Tasks task) => new()
        {
            Id          = task.Id,
            Title       = task.Title,
            Description = task.Description,
            Status      = task.Status,
            Priority    = task.Priority,
            DueDate     = task.DueDate,
            CreatedAt   = task.CreatedAt,
            UpdatedAt   = task.UpdatedAt,
            UserId      = task.UserId,
            OwnerName   = task.User?.FullName ?? task.User?.Email ?? task.UserId,
            OwnerEmail  = task.User?.Email    ?? string.Empty
        };
    }
}
