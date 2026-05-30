using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _10Pearls_Web_Project.Test.Database
{
    /// <summary>
    /// Tests for verifying Database Context, Migrations, and Seeders.
    /// </summary>
    public class DatabaseTests
    {
        #region Connection Tests

        /// <summary>
        /// Verifies that the DbContext can successfully connect to the in-memory database,
        /// write data, and read it back.
        /// </summary>
        [Fact]
        public async Task DbContext_ShouldConnectAndPersistData_InInMemoryDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "ConnectionTestDb_" + Guid.NewGuid())
                .Options;

            var loggerMock = new Mock<ILogger<ApplicationDBContext>>();
            using var context = new ApplicationDBContext(options, loggerMock.Object);

            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "db_test@example.com",
                Email = "db_test@example.com",
                FullName = "Database Connection Test User"
            };

            // Act
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Assert
            var retrievedUser = await context.Users.FindAsync(user.Id);
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Email.Should().Be(user.Email);
            retrievedUser.FullName.Should().Be(user.FullName);
        }

        #endregion

        #region Migration Tests

        /// <summary>
        /// Verifies that all EF Core migrations apply successfully without throwing exceptions
        /// when run against a relational database provider (using SQLite in-memory).
        /// </summary>
        [Fact]
        public async Task Migrations_ShouldApplySuccessfully_OnRelationalDatabase()
        {
            // Arrange
            // SQLite in-memory needs an open connection to keep the database alive
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            try
            {
                var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                    .UseSqlite(connection)
                    .Options;

                var loggerMock = new Mock<ILogger<ApplicationDBContext>>();
                using var context = new ApplicationDBContext(options, loggerMock.Object);

                // Act
                Func<Task> act = () => context.Database.MigrateAsync();

                // Assert
                await act.Should().NotThrowAsync();

                // Verify the migrations were actually applied by checking the migrations table
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var list = appliedMigrations.ToList();

                list.Should().Contain(migration => migration.Contains("IntialMIgration"));
                list.Should().Contain(migration => migration.Contains("Phase2_TaskEnumsAndAuditFields"));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        #endregion

        #region RoleSeeder Tests

        /// <summary>
        /// Verifies that RoleSeeder creates both 'Admin' and 'User' roles in the database
        /// when they do not already exist.
        /// </summary>
        [Fact]
        public async Task SeedAsync_ShouldCreateRoles_WhenRolesDoNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "SeederTestDb_" + Guid.NewGuid())
                .Options;

            var loggerMock = new Mock<ILogger<ApplicationDBContext>>();
            using var context = new ApplicationDBContext(options, loggerMock.Object);

            // Construct real RoleManager over in-memory db
            var roleStore = new RoleStore<IdentityRole>(context);
            var roleManager = new RoleManager<IdentityRole>(
                roleStore,
                new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object);

            var seederLoggerMock = new Mock<ILogger>();

            // Ensure database has no roles initially
            (await context.Roles.AnyAsync()).Should().BeFalse();

            // Act
            await RoleSeeder.SeedAsync(roleManager, seederLoggerMock.Object);

            // Assert
            var roles = await context.Roles.ToListAsync();
            roles.Should().HaveCount(2);
            roles.Select(r => r.Name).Should().Contain("Admin");
            roles.Select(r => r.Name).Should().Contain("User");
        }

        #endregion
    }
}
