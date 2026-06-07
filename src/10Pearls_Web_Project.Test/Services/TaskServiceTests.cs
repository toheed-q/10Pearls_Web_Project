using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Hubs;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _10Pearls_Web_Project.Test.Services
{
    /// <summary>
    /// Unit tests for TaskService using EF Core InMemory database.
    /// Each test runs in an isolated database (new Guid name) so tests never interfere.
    /// ApplicationUser rows must be seeded for any test that exercises an Include(t => t.User)
    /// path — EF Core InMemory performs an inner join for required navigation properties.
    /// </summary>
    public class TaskServiceTests
    {
        private readonly Mock<ILogger<TaskService>> _loggerMock;

        public TaskServiceTests()
        {
            _loggerMock = new Mock<ILogger<TaskService>>();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static ApplicationDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDBContext(options, new Mock<ILogger<ApplicationDBContext>>().Object);
        }

        /// <summary>Mocks IHubContext so SignalR calls inside TaskService are no-ops.</summary>
        private static IHubContext<TaskHub> CreateHubMock()
        {
            var hub     = new Mock<IHubContext<TaskHub>>();
            var clients = new Mock<IHubClients>();
            var proxy   = new Mock<IClientProxy>();

            hub.Setup(h => h.Clients).Returns(clients.Object);
            clients.Setup(c => c.Group(It.IsAny<string>())).Returns(proxy.Object);
            proxy.Setup(p => p.SendCoreAsync(
                    It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            return hub.Object;
        }

        /// <summary>
        /// Seeds a minimal ApplicationUser so EF Core Include(t => t.User) can satisfy
        /// the required navigation property and return the task row.
        /// </summary>
        private static async Task SeedUserAsync(ApplicationDBContext db, string userId)
        {
            db.Users.Add(new ApplicationUser
            {
                Id                 = userId,
                UserName           = $"{userId}@test.com",
                NormalizedUserName = $"{userId}@test.com".ToUpper(),
                Email              = $"{userId}@test.com",
                NormalizedEmail    = $"{userId}@test.com".ToUpper(),
                FullName           = $"User {userId}",
                SecurityStamp      = Guid.NewGuid().ToString()
            });
            await db.SaveChangesAsync();
        }

        // ── CreateTaskAsync ───────────────────────────────────────────────────

        /// <summary>
        /// Verifies that CreateTaskAsync saves the task with correct data and UTC timestamps.
        /// </summary>
        [Fact]
        public async Task CreateTaskAsync_ShouldCreateTaskAndSetTimestamps_WhenCalledWithValidData()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc = new TaskService(db, _loggerMock.Object, CreateHubMock());

            var dto = new CreateTaskDTO
            {
                Title       = "Test Task",
                Description = "Task Description",
                Status      = AppTaskStatus.Pending,
                Priority    = AppTaskPriority.High,
                DueDate     = DateTime.UtcNow.AddDays(7)
            };
            var start = DateTime.UtcNow;

            var result = await svc.CreateTaskAsync("user-1", dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Title.Should().Be(dto.Title);
            result.Description.Should().Be(dto.Description);
            result.Status.Should().Be(dto.Status);
            result.Priority.Should().Be(dto.Priority);
            result.UserId.Should().Be("user-1");
            result.CreatedAt.Should().BeOnOrAfter(start);
            result.UpdatedAt.Should().BeOnOrAfter(start);

            (await db.Tasks.FirstOrDefaultAsync(t => t.Id == result.Id))
                .Should().NotBeNull();
        }

        // ── GetTasksAsync ─────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a regular user only sees their own tasks.
        /// </summary>
        [Fact]
        public async Task GetTasksAsync_ShouldReturnOnlyUserTasks_WhenUserIsNotAdmin()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            await SeedUserAsync(db, "user-2");
            var svc = new TaskService(db, _loggerMock.Object, CreateHubMock());

            db.Tasks.AddRange(
                new Tasks { Id = Guid.NewGuid(), Title = "U1-T1", UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "U1-T2", UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "U2-T1", UserId = "user-2" });
            await db.SaveChangesAsync();

            var result = await svc.GetTasksAsync("user-1", isAdmin: false, page: 1, pageSize: 100, status: null, search: null, sortOrder: "desc");

            result.Items.Should().HaveCount(2);
            result.Items.All(t => t.UserId == "user-1").Should().BeTrue();
        }

        /// <summary>
        /// Verifies that an Admin receives all tasks across all users.
        /// </summary>
        [Fact]
        public async Task GetTasksAsync_ShouldReturnAllTasks_WhenUserIsAdmin()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            await SeedUserAsync(db, "user-2");
            var svc = new TaskService(db, _loggerMock.Object, CreateHubMock());

            db.Tasks.AddRange(
                new Tasks { Id = Guid.NewGuid(), Title = "T1", UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "T2", UserId = "user-2" });
            await db.SaveChangesAsync();

            var result = await svc.GetTasksAsync("user-1", isAdmin: true, page: 1, pageSize: 100, status: null, search: null, sortOrder: "desc");

            result.Items.Should().HaveCount(2);
        }

        // ── GetTaskByIdAsync ──────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the task owner can retrieve their task by ID.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenUserIsOwner()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "My Task", UserId = "user-1" });
            await db.SaveChangesAsync();

            var result = await svc.GetTaskByIdAsync("user-1", taskId, isAdmin: false);

            result.Should().NotBeNull();
            result!.Id.Should().Be(taskId);
            result.Title.Should().Be("My Task");
        }

        /// <summary>
        /// Verifies that a non-owner non-admin cannot access another user's task.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnNull_WhenUserIsNotOwnerAndNotAdmin()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "Secret Task", UserId = "user-1" });
            await db.SaveChangesAsync();

            var result = await svc.GetTaskByIdAsync("user-2", taskId, isAdmin: false);

            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that an Admin can access any task regardless of ownership.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenUserIsNotOwnerButIsAdmin()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "User Task", UserId = "user-1" });
            await db.SaveChangesAsync();

            var result = await svc.GetTaskByIdAsync("admin-1", taskId, isAdmin: true);

            result.Should().NotBeNull();
            result!.Id.Should().Be(taskId);
        }

        // ── UpdateTaskAsync ───────────────────────────────────────────────────

        /// <summary>
        /// Verifies that only the fields present in the DTO are applied; others are preserved.
        /// </summary>
        [Fact]
        public async Task UpdateTaskAsync_ShouldApplyPartialUpdates_WhenUserIsOwner()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc           = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId        = Guid.NewGuid();
            var originalDate  = DateTime.UtcNow.AddDays(1);

            db.Tasks.Add(new Tasks
            {
                Id          = taskId,
                Title       = "Original Title",
                Description = "Original Description",
                Status      = AppTaskStatus.Pending,
                Priority    = AppTaskPriority.Low,
                DueDate     = originalDate,
                UserId      = "user-1"
            });
            await db.SaveChangesAsync();

            var updateDto = new UpdateTaskDTO
            {
                Title       = "Updated Title",
                Status      = AppTaskStatus.InProgress,
                Description = null,   // keep original
                Priority    = null,   // keep original
                DueDate     = null    // keep original
            };

            var result = await svc.UpdateTaskAsync("user-1", taskId, updateDto, isAdmin: false);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Updated Title");
            result.Status.Should().Be(AppTaskStatus.InProgress);
            result.Description.Should().Be("Original Description");
            result.Priority.Should().Be(AppTaskPriority.Low);
            result.DueDate.Should().Be(originalDate);
        }

        /// <summary>
        /// Verifies that a non-owner cannot update a task and the database stays unchanged.
        /// </summary>
        [Fact]
        public async Task UpdateTaskAsync_ShouldReturnNull_WhenUserIsNotOwner()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "Original Title", UserId = "user-1" });
            await db.SaveChangesAsync();

            var result = await svc.UpdateTaskAsync("user-2", taskId, new UpdateTaskDTO { Title = "Hacked" }, isAdmin: false);

            result.Should().BeNull();
            (await db.Tasks.FindAsync(taskId))!.Title.Should().Be("Original Title");
        }

        // ── DeleteTaskAsync ───────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the task owner can delete their task and it is removed from the DB.
        /// </summary>
        [Fact]
        public async Task DeleteTaskAsync_ShouldDeleteTask_WhenUserIsOwner()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "Task to Delete", UserId = "user-1" });
            await db.SaveChangesAsync();

            var deleted = await svc.DeleteTaskAsync("user-1", taskId, isAdmin: false);

            deleted.Should().BeTrue();
            (await db.Tasks.FindAsync(taskId)).Should().BeNull();
        }

        /// <summary>
        /// Verifies that a non-owner cannot delete a task and it remains in the DB.
        /// </summary>
        [Fact]
        public async Task DeleteTaskAsync_ShouldNotDeleteTask_WhenUserIsNotOwner()
        {
            using var db = CreateDbContext();
            await SeedUserAsync(db, "user-1");
            var svc    = new TaskService(db, _loggerMock.Object, CreateHubMock());
            var taskId = Guid.NewGuid();

            db.Tasks.Add(new Tasks { Id = taskId, Title = "Task to Keep", UserId = "user-1" });
            await db.SaveChangesAsync();

            var deleted = await svc.DeleteTaskAsync("user-2", taskId, isAdmin: false);

            deleted.Should().BeFalse();
            (await db.Tasks.FindAsync(taskId)).Should().NotBeNull();
        }

        // ── GetStatsAsync ─────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that stats counts are correct for a regular user (only their tasks counted).
        /// </summary>
        [Fact]
        public async Task GetStatsAsync_ShouldCalculateStatsCorrectly_ForRegularUser()
        {
            using var db = CreateDbContext();
            var svc = new TaskService(db, _loggerMock.Object, CreateHubMock());

            db.Tasks.AddRange(
                new Tasks { Id = Guid.NewGuid(), Title = "P1", Status = AppTaskStatus.Pending,    UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "P2", Status = AppTaskStatus.Pending,    UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "I1", Status = AppTaskStatus.InProgress, UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "C1", Status = AppTaskStatus.Completed,  UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "O1", Status = AppTaskStatus.Completed,  UserId = "user-2" });
            await db.SaveChangesAsync();

            var stats = await svc.GetStatsAsync("user-1", isAdmin: false);

            stats.Total.Should().Be(4);
            stats.Pending.Should().Be(2);
            stats.InProgress.Should().Be(1);
            stats.Completed.Should().Be(1);
        }

        /// <summary>
        /// Verifies that an Admin's stats cover all tasks in the system.
        /// </summary>
        [Fact]
        public async Task GetStatsAsync_ShouldCalculateStatsCorrectly_ForAdmin()
        {
            using var db = CreateDbContext();
            var svc = new TaskService(db, _loggerMock.Object, CreateHubMock());

            db.Tasks.AddRange(
                new Tasks { Id = Guid.NewGuid(), Title = "T1", Status = AppTaskStatus.Pending,    UserId = "user-1" },
                new Tasks { Id = Guid.NewGuid(), Title = "T2", Status = AppTaskStatus.InProgress, UserId = "user-2" },
                new Tasks { Id = Guid.NewGuid(), Title = "T3", Status = AppTaskStatus.Completed,  UserId = "user-3" });
            await db.SaveChangesAsync();

            var stats = await svc.GetStatsAsync("admin-1", isAdmin: true);

            stats.Total.Should().Be(3);
            stats.Pending.Should().Be(1);
            stats.InProgress.Should().Be(1);
            stats.Completed.Should().Be(1);
        }
    }
}
