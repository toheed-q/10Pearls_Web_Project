using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10Pearls_Web_Project.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ITaskExportService _exportService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskService taskService,
            ITaskExportService exportService,
            ILogger<TasksController> logger)
        {
            _taskService   = taskService;
            _exportService = exportService;
            _logger        = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole(Roles.Admin);

        // GET /api/tasks/export/csv
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv()
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            var (content, fileName) = await _exportService.ExportCsvAsync(userId, IsAdmin);
            return File(content, "text/csv", fileName);
        }

        // GET /api/tasks/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            var stats = await _taskService.GetStatsAsync(userId, IsAdmin);
            return Ok(stats);
        }

        // POST /api/tasks
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDTO dto)
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            _logger.LogInformation("Create task request from User {UserId}", userId);

            var task = await _taskService.CreateTaskAsync(userId, dto);
            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }

        // GET /api/tasks
        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            var tasks = await _taskService.GetTasksAsync(userId, IsAdmin);
            return Ok(tasks);
        }

        // GET /api/tasks/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTaskById(Guid id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            var task = await _taskService.GetTaskByIdAsync(userId, id, IsAdmin);
            return task == null ? NotFound(new { message = "Task not found" }) : Ok(task);
        }

        // PUT /api/tasks/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDTO dto)
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            _logger.LogInformation("Update task {TaskId} request from User {UserId}", id, userId);

            var task = await _taskService.UpdateTaskAsync(userId, id, dto, IsAdmin);
            return task == null ? NotFound(new { message = "Task not found" }) : Ok(task);
        }

        // DELETE /api/tasks/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Unauthorized();

            _logger.LogInformation("Delete task {TaskId} request from User {UserId}", id, userId);

            var deleted = await _taskService.DeleteTaskAsync(userId, id, IsAdmin);
            return deleted ? NoContent() : NotFound(new { message = "Task not found" });
        }
    }
}
