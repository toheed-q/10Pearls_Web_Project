using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace _10Pearls_Web_Project.Server.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDBContext db, ILogger<TaskService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<TaskResponseDTO> CreateTaskAsync(string userId, CreateTaskDTO dto)
        {
            var task = new Tasks
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} created for User {UserId}", task.Id, userId);
            return MapToDTO(task);
        }

        public async Task<List<TaskResponseDTO>> GetTasksAsync(string userId, bool isAdmin)
        {
            // Admin sees all tasks; regular users see only their own
            var query = isAdmin
                ? _db.Tasks.AsQueryable()
                : _db.Tasks.Where(t => t.UserId == userId);

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => MapToDTO(t))
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} tasks for User {UserId} (isAdmin={IsAdmin})",
                tasks.Count, userId, isAdmin);

            return tasks;
        }

        public async Task<TaskResponseDTO?> GetTaskByIdAsync(string userId, Guid taskId, bool isAdmin)
        {
            // Admin can fetch any task by ID; user only their own
            var task = isAdmin
                ? await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId)
                : await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for User {UserId}", taskId, userId);
                return null;
            }

            return MapToDTO(task);
        }

        public async Task<TaskResponseDTO?> UpdateTaskAsync(string userId, Guid taskId, UpdateTaskDTO dto)
        {
            // Only the task owner can update — admins use their own tasks or manage via other means
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Update failed — Task {TaskId} not found for User {UserId}", taskId, userId);
                return null;
            }

            if (dto.Title != null) task.Title = dto.Title;
            if (dto.Description != null) task.Description = dto.Description;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate.Value;
            if (dto.Status.HasValue) task.Status = dto.Status.Value;
            if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} updated for User {UserId}", taskId, userId);
            return MapToDTO(task);
        }

        public async Task<bool> DeleteTaskAsync(string userId, Guid taskId)
        {
            // Only the task owner can delete
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Delete failed — Task {TaskId} not found for User {UserId}", taskId, userId);
                return false;
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} deleted by User {UserId}", taskId, userId);
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
                    Total = g.Count(),
                    Pending = g.Count(t => t.Status == AppTaskStatus.Pending),
                    InProgress = g.Count(t => t.Status == AppTaskStatus.InProgress),
                    Completed = g.Count(t => t.Status == AppTaskStatus.Completed)
                })
                .FirstOrDefaultAsync();

            // No tasks yet — return zeroes
            return stats ?? new TaskStatsDTO();
        }

        private static TaskResponseDTO MapToDTO(Tasks task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            UserId = task.UserId
        };
    }
}
