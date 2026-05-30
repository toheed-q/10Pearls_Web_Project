using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.DTOs;
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
    /// Integration tests for AuthController.
    /// Uses CustomWebApplicationFactory to bootstrap the in-memory server.
    /// </summary>
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Helper to seed a user with a specific role and return an authenticated HttpClient.
        /// </summary>
        private async Task<(HttpClient Client, ApplicationUser User)> CreateAuthenticatedClientAsync(string email, string role, string password = "Password123!")
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
                    FullName = "Integration Test User"
                };
                await userManager.CreateAsync(user, password);
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

        #region Register Endpoints

        /// <summary>
        /// Verifies that POST api/auth/register returns 200 OK and registers the user.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnOk_WhenRegistrationIsValid()
        {
            // Arrange
            var client = _factory.CreateClient();
            var registerDto = new RegisterDTO
            {
                Email = $"register_{Guid.NewGuid()}@example.com",
                Password = "Password123!",
                FullName = "Register Test"
            };

            // Act
            var response = await client.PostAsJsonAsync("api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            body.Should().NotBeNull();
            body!["message"].Should().Be("User registered successfully");
        }

        /// <summary>
        /// Verifies that POST api/auth/register returns 400 BadRequest when email already exists.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"duplicate_{Guid.NewGuid()}@example.com";
            var registerDto = new RegisterDTO
            {
                Email = email,
                Password = "Password123!",
                FullName = "Register Duplicate"
            };

            // Seed user first
            await client.PostAsJsonAsync("api/auth/register", registerDto);

            // Act: register again with same email
            var response = await client.PostAsJsonAsync("api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            body.Should().NotBeNull();
            body!["message"].Should().Be("User already exists");
        }

        #endregion

        #region Login Endpoints

        /// <summary>
        /// Verifies that POST api/auth/login returns 200 OK and JWT auth details when valid credentials are provided.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnOkAndToken_WhenCredentialsAreValid()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"login_{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            // Seed user via registration
            await client.PostAsJsonAsync("api/auth/register", new RegisterDTO
            {
                Email = email,
                Password = password,
                FullName = "Login User"
            });

            var loginDto = new LoginDTO { Email = email, Password = password };

            // Act
            var response = await client.PostAsJsonAsync("api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrWhiteSpace();
            authResponse.Email.Should().Be(email);
            authResponse.FullName.Should().Be("Login User");
            authResponse.Role.Should().Be("User"); // default role
        }

        /// <summary>
        /// Verifies that POST api/auth/login returns 401 Unauthorized when invalid credentials are provided.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var client = _factory.CreateClient();
            var loginDto = new LoginDTO
            {
                Email = "nonexistent_login@example.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await client.PostAsJsonAsync("api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            body.Should().NotBeNull();
            body!["message"].Should().Be("Invalid credentials");
        }

        #endregion

        #region Profile Endpoints

        /// <summary>
        /// Verifies that GET api/auth/profile returns 200 OK and the current user's profile when authenticated.
        /// </summary>
        [Fact]
        public async Task GetProfile_ShouldReturnOk_WhenAuthenticated()
        {
            // Arrange
            var email = $"profile_{Guid.NewGuid()}@example.com";
            var (client, user) = await CreateAuthenticatedClientAsync(email, "User");

            // Act
            var response = await client.GetAsync("api/auth/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var profile = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
            profile.Should().NotBeNull();
            profile!.Id.Should().Be(user.Id);
            profile.Email.Should().Be(email);
        }

        /// <summary>
        /// Verifies that GET api/auth/profile returns 401 Unauthorized when unauthenticated.
        /// </summary>
        [Fact]
        public async Task GetProfile_ShouldReturnUnauthorized_WhenUnauthenticated()
        {
            // Arrange
            var client = _factory.CreateClient(); // No auth header

            // Act
            var response = await client.GetAsync("api/auth/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Promote Endpoints

        /// <summary>
        /// Verifies that POST api/auth/promote returns 200 OK when called by an Admin.
        /// </summary>
        [Fact]
        public async Task PromoteToAdmin_ShouldReturnOk_WhenCalledByAdmin()
        {
            // Arrange
            // Create user to promote
            var userEmail = $"user_to_promote_{Guid.NewGuid()}@example.com";
            var client = _factory.CreateClient();
            await client.PostAsJsonAsync("api/auth/register", new RegisterDTO
            {
                Email = userEmail,
                Password = "Password123!",
                FullName = "Normal User"
            });

            // Create admin client
            var adminEmail = $"admin_promoter_{Guid.NewGuid()}@example.com";
            var (adminClient, _) = await CreateAuthenticatedClientAsync(adminEmail, "Admin");

            var promoteDto = new PromoteDTO { Email = userEmail };

            // Act
            var response = await adminClient.PostAsJsonAsync("api/auth/promote", promoteDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            body.Should().NotBeNull();
            body!["message"].Should().Be($"{userEmail} has been promoted to Admin");
        }

        /// <summary>
        /// Verifies that POST api/auth/promote returns 403 Forbidden when called by a non-Admin user.
        /// </summary>
        [Fact]
        public async Task PromoteToAdmin_ShouldReturnForbidden_WhenCalledByRegularUser()
        {
            // Arrange
            var userEmail = $"user_{Guid.NewGuid()}@example.com";
            var (client, _) = await CreateAuthenticatedClientAsync(userEmail, "User");

            var promoteDto = new PromoteDTO { Email = "target@example.com" };

            // Act
            var response = await client.PostAsJsonAsync("api/auth/promote", promoteDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Verifies that POST api/auth/promote returns 401 Unauthorized when unauthenticated.
        /// </summary>
        [Fact]
        public async Task PromoteToAdmin_ShouldReturnUnauthorized_WhenUnauthenticated()
        {
            // Arrange
            var client = _factory.CreateClient(); // No auth header
            var promoteDto = new PromoteDTO { Email = "target@example.com" };

            // Act
            var response = await client.PostAsJsonAsync("api/auth/promote", promoteDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion
    }
}
