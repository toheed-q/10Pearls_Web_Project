using System.Text;
using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.Services
{
    public class TaskExportService : ITaskExportService
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskExportService> _logger;

        public TaskExportService(ITaskService taskService, ILogger<TaskExportService> logger)
        {
            _taskService = taskService;
            _logger      = logger;
        }

        public async Task<(byte[] Content, string FileName)> ExportCsvAsync(string userId, bool isAdmin)
        {
            _logger.LogInformation("User {UserId} requested CSV export (isAdmin={IsAdmin})", userId, isAdmin);

            var tasks = await _taskService.GetTasksAsync(userId, isAdmin);

            var csv = BuildCsv(tasks);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var fileName = $"tasks-export-{DateTime.UtcNow:yyyyMMdd}.csv";

            _logger.LogInformation("User {UserId} exported {Count} tasks", userId, tasks.Count);

            return (bytes, fileName);
        }

        // ── Private helpers ────────────────────────────────────────────────

        private static string BuildCsv(List<TaskResponseDTO> tasks)
        {
            var sb = new StringBuilder();

            // Header row
            sb.AppendLine("Id,Title,Description,Status,Priority,DueDate,CreatedAt,UpdatedAt,OwnerName,OwnerEmail");

            foreach (var t in tasks)
            {
                sb.AppendLine(string.Join(',',
                    Escape(t.Id.ToString()),
                    Escape(t.Title),
                    Escape(t.Description),
                    Escape(t.Status.ToString()),
                    Escape(t.Priority.ToString()),
                    Escape(t.DueDate.ToString("yyyy-MM-dd")),
                    Escape(t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    Escape(t.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    Escape(t.OwnerName),
                    Escape(t.OwnerEmail)
                ));
            }

            return sb.ToString();
        }

        /// <summary>
        /// RFC 4180 CSV escaping — wraps field in quotes if it contains
        /// a comma, quote, or newline; doubles any internal quotes.
        /// </summary>
        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}
