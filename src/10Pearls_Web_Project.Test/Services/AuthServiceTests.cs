using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.DTOs;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _10Pearls_Web_Project.Test.Services
{
    /// <summary>
    /// Unit tests for verifying the functionality of AuthService.
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<JWTService> _jwtServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Set up UserManager mock
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Set up SignInManager mock
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

            // Set up JWTService mock
            var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _jwtServiceMock = new Mock<JWTService>(config.Object);

            // Set up Logger mock
            _loggerMock = new Mock<ILogger<AuthService>>();

            // Create AuthService instance with mocked dependencies
            _authService = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _jwtServiceMock.Object,
                _loggerMock.Object);
        }

        #region RegisterAsync Tests

        /// <summary>
        /// Verifies that RegisterAsync returns failure when the user email already exists.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenUserEmailAlreadyExists()
        {
            // Arrange
            var dto = new RegisterDTO
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FullName = "Existing User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(new ApplicationUser { Email = dto.Email });

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be("User already exists");
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Verifies that RegisterAsync returns success, creates the user, and assigns the default "User" role.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenUserCreationSucceeds()
        {
            // Arrange
            var dto = new RegisterDTO
            {
                Email = "new@example.com",
                Password = "Password123!",
                FullName = "New User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
            _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == dto.Email && u.FullName == dto.FullName), dto.Password), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Email == dto.Email), "User"), Times.Once);
        }

        /// <summary>
        /// Verifies that RegisterAsync returns failure and list of errors when the UserManager returns validation/creation errors.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenUserCreationFails()
        {
            // Arrange
            var dto = new RegisterDTO
            {
                Email = "invalid@example.com",
                Password = "short",
                FullName = "Invalid User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            var errors = new[]
            {
                new IdentityError { Description = "Password too short." },
                new IdentityError { Description = "Password requires non-alphanumeric character." }
            };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Password too short., Password requires non-alphanumeric character.");
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region LoginAsync Tests

        /// <summary>
        /// Verifies that LoginAsync returns failure when the user email is not found.
        /// </summary>
        [Fact]
        public async Task LoginAsync_ShouldReturnFailure_WhenUserDoesNotExist()
        {
            // Arrange
            var dto = new LoginDTO
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Error.Should().Be("Invalid credentials");
            _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        /// <summary>
        /// Verifies that LoginAsync returns failure when password is incorrect.
        /// </summary>
        [Fact]
        public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsIncorrect()
        {
            // Arrange
            var dto = new LoginDTO
            {
                Email = "user@example.com",
                Password = "WrongPassword!"
            };

            var user = new ApplicationUser { Email = dto.Email };
            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, false))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Error.Should().Be("Invalid credentials");
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        /// <summary>
        /// Verifies that LoginAsync returns success, JWT token, and user info (resolving Admin role priority)
        /// when credentials are correct.
        /// </summary>
        [Fact]
        public async Task LoginAsync_ShouldReturnSuccessAndToken_WhenCredentialsAreValid()
        {
            // Arrange
            var dto = new LoginDTO
            {
                Email = "admin@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "admin-id",
                Email = dto.Email,
                FullName = "Admin User"
            };

            var roles = new List<string> { "User", "Admin" };
            var generatedToken = "jwt-mock-token-string";

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, false))
                .ReturnsAsync(SignInResult.Success);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            _jwtServiceMock.Setup(x => x.GenerateToken(user, roles))
                .Returns(generatedToken);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be(generatedToken);
            result.Data.Id.Should().Be(user.Id);
            result.Data.Email.Should().Be(user.Email);
            result.Data.FullName.Should().Be(user.FullName);
            result.Data.Role.Should().Be("Admin"); // Admin role prioritized over User
        }

        #endregion

        #region PromoteToAdminAsync Tests

        /// <summary>
        /// Verifies PromoteToAdminAsync returns failure when user is not found.
        /// </summary>
        [Fact]
        public async Task PromoteToAdminAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _authService.PromoteToAdminAsync(email);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be($"No user found with email '{email}'");
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Verifies PromoteToAdminAsync returns failure when user is already an Admin.
        /// </summary>
        [Fact]
        public async Task PromoteToAdminAsync_ShouldReturnFailure_WhenUserIsAlreadyAdmin()
        {
            // Arrange
            var email = "admin@example.com";
            var user = new ApplicationUser { Email = email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.PromoteToAdminAsync(email);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be("User is already an Admin");
            _userManagerMock.Verify(x => x.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Verifies PromoteToAdminAsync successfully removes 'User' role and adds 'Admin' role to the target user.
        /// </summary>
        [Fact]
        public async Task PromoteToAdminAsync_ShouldSucceed_WhenUserIsNotAdmin()
        {
            // Arrange
            var email = "user@example.com";
            var user = new ApplicationUser { Id = "user-id", Email = email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _userManagerMock.Setup(x => x.RemoveFromRoleAsync(user, "User"))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.PromoteToAdminAsync(email);

            // Assert
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
            _userManagerMock.Verify(x => x.RemoveFromRoleAsync(user, "User"), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
        }

        #endregion
    }
}
