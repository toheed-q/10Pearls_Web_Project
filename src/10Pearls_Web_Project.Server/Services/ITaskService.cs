using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public interface ITaskService
    {
        Task<TaskResponseDTO> CreateTaskAsync(string userId, CreateTaskDTO dto);
        Task<List<TaskResponseDTO>> GetTasksAsync(string userId, bool isAdmin);
        Task<TaskResponseDTO?> GetTaskByIdAsync(string userId, Guid taskId, bool isAdmin);
        Task<TaskResponseDTO?> UpdateTaskAsync(string userId, Guid taskId, UpdateTaskDTO dto);
        Task<bool> DeleteTaskAsync(string userId, Guid taskId);
        Task<TaskStatsDTO> GetStatsAsync(string userId, bool isAdmin);
    }
}
