using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Enums;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _10Pearls_Web_Project.Test.Services
{
    /// <summary>
    /// Unit tests for verifying the functionality of TaskService.
    /// Uses Entity Framework Core In-Memory database for isolated testing.
    /// </summary>
    public class TaskServiceTests
    {
        private readonly Mock<ILogger<TaskService>> _loggerMock;

        public TaskServiceTests()
        {
            _loggerMock = new Mock<ILogger<TaskService>>();
        }

        /// <summary>
        /// Helper to create a new, isolated ApplicationDBContext using InMemoryDatabase.
        /// </summary>
        private static ApplicationDBContext CreateDbContext()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var dbLoggerMock = new Mock<ILogger<ApplicationDBContext>>();
            return new ApplicationDBContext(options, dbLoggerMock.Object);
        }

        #region CreateTaskAsync Tests

        /// <summary>
        /// Verifies that CreateTaskAsync correctly saves a new task to the database
        /// with the given user ID and correct UTC timestamps.
        /// </summary>
        [Fact]
        public async Task CreateTaskAsync_ShouldCreateTaskAndSetTimestamps_WhenCalledWithValidData()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var userId = "user-1";
            var dto = new CreateTaskDTO
            {
                Title = "Test Task",
                Description = "Task Description",
                Status = AppTaskStatus.Pending,
                Priority = AppTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var testStartTime = DateTime.UtcNow;

            // Act
            var result = await taskService.CreateTaskAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Title.Should().Be(dto.Title);
            result.Description.Should().Be(dto.Description);
            result.Status.Should().Be(dto.Status);
            result.Priority.Should().Be(dto.Priority);
            result.DueDate.Should().Be(dto.DueDate);
            result.UserId.Should().Be(userId);
            result.CreatedAt.Should().BeOnOrAfter(testStartTime);
            result.UpdatedAt.Should().BeOnOrAfter(testStartTime);

            // Verify database persistence
            var dbTask = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == result.Id);
            dbTask.Should().NotBeNull();
            dbTask!.Title.Should().Be(dto.Title);
        }

        #endregion

        #region GetTasksAsync Tests

        /// <summary>
        /// Verifies that GetTasksAsync returns only the tasks belonging to the specified user
        /// when the caller is not an Admin.
        /// </summary>
        [Fact]
        public async Task GetTasksAsync_ShouldReturnOnlyUserTasks_WhenUserIsNotAdmin()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var user1 = "user-1";
            var user2 = "user-2";

            dbContext.Tasks.AddRange(new List<Tasks>
            {
                new() { Id = Guid.NewGuid(), Title = "Task 1", UserId = user1 },
                new() { Id = Guid.NewGuid(), Title = "Task 2", UserId = user1 },
                new() { Id = Guid.NewGuid(), Title = "Task 3", UserId = user2 }
            });
            await dbContext.SaveChangesAsync();

            // Act
            var tasks = await taskService.GetTasksAsync(user1, isAdmin: false);

            // Assert
            tasks.Should().HaveCount(2);
            tasks.All(t => t.UserId == user1).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that GetTasksAsync returns all tasks from the database
        /// when the caller is an Admin.
        /// </summary>
        [Fact]
        public async Task GetTasksAsync_ShouldReturnAllTasks_WhenUserIsAdmin()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var user1 = "user-1";
            var user2 = "user-2";

            dbContext.Tasks.AddRange(new List<Tasks>
            {
                new() { Id = Guid.NewGuid(), Title = "Task 1", UserId = user1 },
                new() { Id = Guid.NewGuid(), Title = "Task 2", UserId = user2 }
            });
            await dbContext.SaveChangesAsync();

            // Act
            var tasks = await taskService.GetTasksAsync(user1, isAdmin: true);

            // Assert
            tasks.Should().HaveCount(2);
        }

        #endregion

        #region GetTaskByIdAsync Tests

        /// <summary>
        /// Verifies that GetTaskByIdAsync returns the task when the owner requests it.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenUserIsOwner()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var userId = "user-1";
            var taskId = Guid.NewGuid();

            dbContext.Tasks.Add(new Tasks { Id = taskId, Title = "My Task", UserId = userId });
            await dbContext.SaveChangesAsync();

            // Act
            var result = await taskService.GetTaskByIdAsync(userId, taskId, isAdmin: false);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(taskId);
            result.Title.Should().Be("My Task");
        }

        /// <summary>
        /// Verifies that GetTaskByIdAsync returns null when a non-owner non-admin requests it.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnNull_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var ownerId = "user-1";
            var requesterId = "user-2";
            var taskId = Guid.NewGuid();

            dbContext.Tasks.Add(new Tasks { Id = taskId, Title = "Secret Task", UserId = ownerId });
            await dbContext.SaveChangesAsync();

            // Act
            var result = await taskService.GetTaskByIdAsync(requesterId, taskId, isAdmin: false);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that GetTaskByIdAsync returns the task when an Admin requests it,
        /// even if the Admin does not own the task.
        /// </summary>
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenUserIsNotOwnerButIsAdmin()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var ownerId = "user-1";
            var adminId = "admin-1";
            var taskId = Guid.NewGuid();

            dbContext.Tasks.Add(new Tasks { Id = taskId, Title = "User Task", UserId = ownerId });
            await dbContext.SaveChangesAsync();

            // Act
            var result = await taskService.GetTaskByIdAsync(adminId, taskId, isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(taskId);
        }

        #endregion

        #region UpdateTaskAsync Tests

        /// <summary>
        /// Verifies that UpdateTaskAsync updates only the fields provided in the DTO,
        /// leaving the other fields untouched.
        /// </summary>
        [Fact]
        public async Task UpdateTaskAsync_ShouldApplyPartialUpdates_WhenUserIsOwner()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var userId = "user-1";
            var taskId = Guid.NewGuid();
            var originalDueDate = DateTime.UtcNow.AddDays(1);

            var task = new Tasks
            {
                Id = taskId,
                Title = "Original Title",
                Description = "Original Description",
                Status = AppTaskStatus.Pending,
                Priority = AppTaskPriority.Low,
                DueDate = originalDueDate,
                UserId = userId
            };
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            // Partial update: Change only title and status, leave description, priority, and duedate alone.
            var updateDto = new UpdateTaskDTO
            {
                Title = "Updated Title",
                Status = AppTaskStatus.InProgress,
                Description = null,
                Priority = null,
                DueDate = null
            };

            // Act
            var result = await taskService.UpdateTaskAsync(userId, taskId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Updated Title");
            result.Status.Should().Be(AppTaskStatus.InProgress);
            result.Description.Should().Be("Original Description"); // Unchanged
            result.Priority.Should().Be(AppTaskPriority.Low); // Unchanged
            result.DueDate.Should().Be(originalDueDate); // Unchanged
        }

        /// <summary>
        /// Verifies that UpdateTaskAsync returns null and does not update the database
        /// when a non-owner tries to update the task.
        /// </summary>
        [Fact]
        public async Task UpdateTaskAsync_ShouldReturnNull_WhenUserIsNotOwner()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var ownerId = "user-1";
            var hackerId = "user-2";
            var taskId = Guid.NewGuid();

            var task = new Tasks
            {
                Id = taskId,
                Title = "Original Title",
                UserId = ownerId
            };
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            var updateDto = new UpdateTaskDTO { Title = "Hacked Title" };

            // Act
            var result = await taskService.UpdateTaskAsync(hackerId, taskId, updateDto);

            // Assert
            result.Should().BeNull();
            
            // Check that db was not updated
            var dbTask = await dbContext.Tasks.FindAsync(taskId);
            dbTask!.Title.Should().Be("Original Title");
        }

        #endregion

        #region DeleteTaskAsync Tests

        /// <summary>
        /// Verifies that DeleteTaskAsync successfully deletes the task when the owner requests it.
        /// </summary>
        [Fact]
        public async Task DeleteTaskAsync_ShouldDeleteTask_WhenUserIsOwner()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var userId = "user-1";
            var taskId = Guid.NewGuid();

            dbContext.Tasks.Add(new Tasks { Id = taskId, Title = "Task to Delete", UserId = userId });
            await dbContext.SaveChangesAsync();

            // Act
            var deleted = await taskService.DeleteTaskAsync(userId, taskId);

            // Assert
            deleted.Should().BeTrue();
            (await dbContext.Tasks.FindAsync(taskId)).Should().BeNull();
        }

        /// <summary>
        /// Verifies that DeleteTaskAsync returns false and does not delete the task
        /// when a non-owner requests deletion.
        /// </summary>
        [Fact]
        public async Task DeleteTaskAsync_ShouldNotDeleteTask_WhenUserIsNotOwner()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var ownerId = "user-1";
            var requesterId = "user-2";
            var taskId = Guid.NewGuid();

            dbContext.Tasks.Add(new Tasks { Id = taskId, Title = "Task to Keep", UserId = ownerId });
            await dbContext.SaveChangesAsync();

            // Act
            var deleted = await taskService.DeleteTaskAsync(requesterId, taskId);

            // Assert
            deleted.Should().BeFalse();
            (await dbContext.Tasks.FindAsync(taskId)).Should().NotBeNull();
        }

        #endregion

        #region GetStatsAsync Tests

        /// <summary>
        /// Verifies that GetStatsAsync calculates the correct status counts for a regular user's tasks.
        /// </summary>
        [Fact]
        public async Task GetStatsAsync_ShouldCalculateStatsCorrectly_ForRegularUser()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var userId = "user-1";
            var otherUserId = "user-2";

            dbContext.Tasks.AddRange(new List<Tasks>
            {
                new() { Id = Guid.NewGuid(), Title = "P1", Status = AppTaskStatus.Pending, UserId = userId },
                new() { Id = Guid.NewGuid(), Title = "P2", Status = AppTaskStatus.Pending, UserId = userId },
                new() { Id = Guid.NewGuid(), Title = "I1", Status = AppTaskStatus.InProgress, UserId = userId },
                new() { Id = Guid.NewGuid(), Title = "C1", Status = AppTaskStatus.Completed, UserId = userId },
                // Other user's tasks (should not be counted for user-1)
                new() { Id = Guid.NewGuid(), Title = "O1", Status = AppTaskStatus.Completed, UserId = otherUserId }
            });
            await dbContext.SaveChangesAsync();

            // Act
            var stats = await taskService.GetStatsAsync(userId, isAdmin: false);

            // Assert
            stats.Should().NotBeNull();
            stats.Total.Should().Be(4);
            stats.Pending.Should().Be(2);
            stats.InProgress.Should().Be(1);
            stats.Completed.Should().Be(1);
        }

        /// <summary>
        /// Verifies that GetStatsAsync calculates the correct status counts across all tasks in the database for an Admin.
        /// </summary>
        [Fact]
        public async Task GetStatsAsync_ShouldCalculateStatsCorrectly_ForAdmin()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var taskService = new TaskService(dbContext, _loggerMock.Object);
            var adminId = "admin-1";

            dbContext.Tasks.AddRange(new List<Tasks>
            {
                new() { Id = Guid.NewGuid(), Title = "T1", Status = AppTaskStatus.Pending, UserId = "user-1" },
                new() { Id = Guid.NewGuid(), Title = "T2", Status = AppTaskStatus.InProgress, UserId = "user-2" },
                new() { Id = Guid.NewGuid(), Title = "T3", Status = AppTaskStatus.Completed, UserId = "user-3" }
            });
            await dbContext.SaveChangesAsync();

            // Act
            var stats = await taskService.GetStatsAsync(adminId, isAdmin: true);

            // Assert
            stats.Should().NotBeNull();
            stats.Total.Should().Be(3);
            stats.Pending.Should().Be(1);
            stats.InProgress.Should().Be(1);
            stats.Completed.Should().Be(1);
        }

        #endregion
    }
}
