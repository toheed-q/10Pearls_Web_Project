using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using _10Pearls_Web_Project.Test.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _10Pearls_Web_Project.Test.Controllers
{
    /// <summary>
    /// Integration tests for TasksController.
    /// Uses CustomWebApplicationFactory to bootstrap the in-memory server.
    /// </summary>
    public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public TasksControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Helper to seed a user and return an authenticated HttpClient.
        /// </summary>
        private async Task<(HttpClient Client, ApplicationUser User)> CreateAuthenticatedClientAsync(string email, string role)
        {
            var client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JWTService>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    Email = email,
                    FullName = "Integration Task Test User"
                };
                await userManager.CreateAsync(user, "Password123!");
            }

            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtService.GenerateToken(user, roles);

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (client, user);
        }

        /// <summary>
        /// Helper to seed tasks directly into the in-memory database.
        /// </summary>
        private async Task SeedTaskAsync(Guid id, string title, string userId, AppTaskStatus status = AppTaskStatus.Pending)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            
            var task = new Tasks
            {
                Id = id,
                Title = title,
                Description = "Seeded Task Description",
                Status = status,
                Priority = AppTaskPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(2),
                UserId = userId
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        #region Authorization Protection Tests

        /// <summary>
        /// Verifies that all endpoints return 401 Unauthorized when an unauthenticated client calls them.
        /// </summary>
        [Fact]
        public async Task Endpoints_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            // Arrange
            var client = _factory.CreateClient(); // No token

            // Act & Assert
            (await client.GetAsync("api/tasks")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.GetAsync($"api/tasks/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PostAsJsonAsync("api/tasks", new CreateTaskDTO())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PutAsJsonAsync($"api/tasks/{Guid.NewGuid()}", new UpdateTaskDTO())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.DeleteAsync($"api/tasks/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.GetAsync("api/tasks/stats")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region CreateTask Tests

        /// <summary>
        /// Verifies that POST api/tasks returns 201 Created and properly maps the response model.
        /// </summary>
        [Fact]
        public async Task CreateTask_ShouldReturnCreated_WhenDataIsValid()
        {
            // Arrange
            var email = $"create_task_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");

            var createTaskDto = new CreateTaskDTO
            {
                Title = "New Task via API",
                Description = "Task created by integration test",
                Status = AppTaskStatus.Pending,
                Priority = AppTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var response = await client.PostAsJsonAsync("api/tasks", createTaskDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var result = await response.Content.ReadFromJsonAsync<TaskResponseDTO>();
            result.Should().NotBeNull();
            result!.Id.Should().NotBeEmpty();
            result.Title.Should().Be(createTaskDto.Title);
            result.Description.Should().Be(createTaskDto.Description);
            result.UserId.Should().Be(user.Id);
            
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Contain($"api/Tasks/{result.Id}");
        }

        #endregion

        #region GetTasks Tests

        /// <summary>
        /// Verifies that GET api/tasks returns only the user's tasks for a regular user.
        /// </summary>
        [Fact]
        public async Task GetTasks_ShouldReturnUserTasksOnly_ForRegularUser()
        {
            // Arrange
            var user1Email = $"user1_tasks_{Guid.NewGuid()}@example.com";
            var user2Email = $"user2_tasks_{Guid.NewGuid()}@example.com";
            
            var (client1, user1) = await CreateAuthenticatedClientAsync(user1Email, "User");
            var (_, user2) = await CreateAuthenticatedClientAsync(user2Email, "User");

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            await SeedTaskAsync(task1Id, "User 1 Task", user1.Id);
            await SeedTaskAsync(task2Id, "User 2 Task", user2.Id);

            // Act
            var response = await client1.GetAsync("api/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponseDTO>>();
            tasks.Should().NotBeNull();
            tasks.Should().ContainSingle();
            tasks![0].Id.Should().Be(task1Id);
            tasks[0].UserId.Should().Be(user1.Id);
        }

        /// <summary>
        /// Verifies that GET api/tasks returns all tasks for an Admin.
        /// </summary>
        [Fact]
        public async Task GetTasks_ShouldReturnAllTasks_ForAdmin()
        {
            // Arrange
            var adminEmail = $"admin_tasks_{Guid.NewGuid()}@example.com";
            var userEmail = $"user_tasks_{Guid.NewGuid()}@example.com";

            var (adminClient, admin) = await CreateAuthenticatedClientAsync(adminEmail, "Admin");
            var (_, user) = await CreateAuthenticatedClientAsync(userEmail, "User");

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            await SeedTaskAsync(task1Id, "Admin Task", admin.Id);
            await SeedTaskAsync(task2Id, "User Task", user.Id);

            // Act
            var response = await adminClient.GetAsync("api/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponseDTO>>();
            tasks.Should().NotBeNull();
            tasks.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        #endregion

        #region GetTaskById Tests

        /// <summary>
        /// Verifies that GET api/tasks/{id} returns 200 OK for the task owner.
        /// </summary>
        [Fact]
        public async Task GetTaskById_ShouldReturnOk_WhenUserIsOwner()
        {
            // Arrange
            var email = $"owner_get_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");
            var taskId = Guid.NewGuid();

            await SeedTaskAsync(taskId, "Task to get", user.Id);

            // Act
            var response = await client.GetAsync($"api/tasks/{taskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var task = await response.Content.ReadFromJsonAsync<TaskResponseDTO>();
            task.Should().NotBeNull();
            task!.Id.Should().Be(taskId);
        }

        /// <summary>
        /// Verifies that GET api/tasks/{id} returns 404 NotFound when a regular user tries to retrieve someone else's task.
        /// </summary>
        [Fact]
        public async Task GetTaskById_ShouldReturnNotFound_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var ownerEmail = $"owner_secret_{Guid.NewGuid()}@example.com";
            var hackerEmail = $"hacker_get_{Guid.NewGuid()}@example.com";

            var (_, owner) = await CreateAuthenticatedClientAsync(ownerEmail, "User");
            var (hackerClient, _) = await CreateAuthenticatedClientAsync(hackerEmail, "User");

            var taskId = Guid.NewGuid();
            await SeedTaskAsync(taskId, "Secret Task", owner.Id);

            // Act
            var response = await hackerClient.GetAsync($"api/tasks/{taskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            error.Should().NotBeNull();
            error!["message"].Should().Be("Task not found");
        }

        /// <summary>
        /// Verifies that GET api/tasks/{id} returns 200 OK for an Admin, even if they don't own the task.
        /// </summary>
        [Fact]
        public async Task GetTaskById_ShouldReturnOk_WhenUserIsAdminAndNotOwner()
        {
            // Arrange
            var ownerEmail = $"owner_task_{Guid.NewGuid()}@example.com";
            var adminEmail = $"admin_get_{Guid.NewGuid()}@example.com";

            var (_, owner) = await CreateAuthenticatedClientAsync(ownerEmail, "User");
            var (adminClient, _) = await CreateAuthenticatedClientAsync(adminEmail, "Admin");

            var taskId = Guid.NewGuid();
            await SeedTaskAsync(taskId, "User Task", owner.Id);

            // Act
            var response = await adminClient.GetAsync($"api/tasks/{taskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var task = await response.Content.ReadFromJsonAsync<TaskResponseDTO>();
            task.Should().NotBeNull();
            task!.Id.Should().Be(taskId);
        }

        #endregion

        #region UpdateTask Tests

        /// <summary>
        /// Verifies that PUT api/tasks/{id} returns 200 OK and updates the task when owner updates it.
        /// </summary>
        [Fact]
        public async Task UpdateTask_ShouldReturnOk_WhenUserIsOwner()
        {
            // Arrange
            var email = $"owner_update_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");
            var taskId = Guid.NewGuid();

            await SeedTaskAsync(taskId, "Old Title", user.Id);

            var updateDto = new UpdateTaskDTO
            {
                Title = "New Title API",
                Status = AppTaskStatus.Completed
            };

            // Act
            var response = await client.PutAsJsonAsync($"api/tasks/{taskId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedTask = await response.Content.ReadFromJsonAsync<TaskResponseDTO>();
            updatedTask.Should().NotBeNull();
            updatedTask!.Title.Should().Be("New Title API");
            updatedTask.Status.Should().Be(AppTaskStatus.Completed);
        }

        /// <summary>
        /// Verifies that PUT api/tasks/{id} returns 404 NotFound when non-owner tries to update it.
        /// </summary>
        [Fact]
        public async Task UpdateTask_ShouldReturnNotFound_WhenUserIsNotOwner()
        {
            // Arrange
            var ownerEmail = $"owner_upd_{Guid.NewGuid()}@example.com";
            var hackerEmail = $"hacker_upd_{Guid.NewGuid()}@example.com";

            var (_, owner) = await CreateAuthenticatedClientAsync(ownerEmail, "User");
            var (hackerClient, _) = await CreateAuthenticatedClientAsync(hackerEmail, "User");

            var taskId = Guid.NewGuid();
            await SeedTaskAsync(taskId, "Original Title", owner.Id);

            var updateDto = new UpdateTaskDTO { Title = "Hacked Title" };

            // Act
            var response = await hackerClient.PutAsJsonAsync($"api/tasks/{taskId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region DeleteTask Tests

        /// <summary>
        /// Verifies that DELETE api/tasks/{id} returns 204 NoContent and deletes the task when called by the owner.
        /// </summary>
        [Fact]
        public async Task DeleteTask_ShouldReturnNoContent_WhenUserIsOwner()
        {
            // Arrange
            var email = $"owner_del_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");
            var taskId = Guid.NewGuid();

            await SeedTaskAsync(taskId, "Task to delete", user.Id);

            // Act
            var response = await client.DeleteAsync($"api/tasks/{taskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Check db directly
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var task = await db.Tasks.FindAsync(taskId);
            task.Should().BeNull();
        }

        /// <summary>
        /// Verifies that DELETE api/tasks/{id} returns 404 NotFound when a non-owner tries to delete the task.
        /// </summary>
        [Fact]
        public async Task DeleteTask_ShouldReturnNotFound_WhenUserIsNotOwner()
        {
            // Arrange
            var ownerEmail = $"owner_delete_{Guid.NewGuid()}@example.com";
            var hackerEmail = $"hacker_delete_{Guid.NewGuid()}@example.com";

            var (_, owner) = await CreateAuthenticatedClientAsync(ownerEmail, "User");
            var (hackerClient, _) = await CreateAuthenticatedClientAsync(hackerEmail, "User");

            var taskId = Guid.NewGuid();
            await SeedTaskAsync(taskId, "Task to protect", owner.Id);

            // Act
            var response = await hackerClient.DeleteAsync($"api/tasks/{taskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetStats Tests

        /// <summary>
        /// Verifies that GET api/tasks/stats returns 200 OK and correct stats.
        /// </summary>
        [Fact]
        public async Task GetStats_ShouldReturnOkAndStats_WhenAuthenticated()
        {
            // Arrange
            var email = $"stats_user_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");

            await SeedTaskAsync(Guid.NewGuid(), "Task 1", user.Id, AppTaskStatus.Pending);
            await SeedTaskAsync(Guid.NewGuid(), "Task 2", user.Id, AppTaskStatus.InProgress);
            await SeedTaskAsync(Guid.NewGuid(), "Task 3", user.Id, AppTaskStatus.Completed);

            // Act
            var response = await client.GetAsync("api/tasks/stats");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var stats = await response.Content.ReadFromJsonAsync<TaskStatsDTO>();
            stats.Should().NotBeNull();
            stats!.Total.Should().Be(3);
            stats.Pending.Should().Be(1);
            stats.InProgress.Should().Be(1);
            stats.Completed.Should().Be(1);
        }

        #endregion
    }
}
