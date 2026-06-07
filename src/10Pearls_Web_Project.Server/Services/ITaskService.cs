using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public interface ITaskService
    {
        Task<TaskResponseDTO> CreateTaskAsync(string userId, CreateTaskDTO dto);
        Task<TaskPagedResponseDTO> GetTasksAsync(
            string userId, bool isAdmin,
            int page, int pageSize,
            string? status, string? search, string sortOrder);
        Task<TaskResponseDTO?> GetTaskByIdAsync(string userId, Guid taskId, bool isAdmin);
        Task<TaskResponseDTO?> UpdateTaskAsync(string userId, Guid taskId, UpdateTaskDTO dto, bool isAdmin);
        Task<bool> DeleteTaskAsync(string userId, Guid taskId, bool isAdmin);
        Task<TaskStatsDTO> GetStatsAsync(string userId, bool isAdmin);
    }
}
