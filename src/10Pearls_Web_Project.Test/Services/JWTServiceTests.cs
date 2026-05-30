using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using _10Pearls_Web_Project.Server.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace _10Pearls_Web_Project.Test.Services
{
    /// <summary>
    /// Unit tests for verifying the functionality of JWTService token generation.
    /// </summary>
    public class JWTServiceTests
    {
        #region GenerateToken Tests

        /// <summary>
        /// Verifies that GenerateToken successfully creates a JWT token string containing
        /// all required user and role claims when a valid configuration is provided.
        /// </summary>
        [Fact]
        public void GenerateToken_ShouldCreateTokenWithCorrectClaims_WhenConfigIsValid()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretKeyLongEnoughToMatchRequirements123!" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:DurationInMinutes", "60" }
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var jwtService = new JWTService(config);

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                FullName = "Test User"
            };

            var roles = new List<string> { "User", "Admin" };

            // Act
            var tokenString = jwtService.GenerateToken(user, roles);

            // Assert
            tokenString.Should().NotBeNullOrWhiteSpace();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            jwtToken.Should().NotBeNull();
            jwtToken.Issuer.Should().Be("TestIssuer");
            jwtToken.Audiences.Should().Contain("TestAudience");

            // Extract claims
            var claims = jwtToken.Claims.ToList();

            var nameIdentifierClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            nameIdentifierClaim.Should().NotBeNull();
            nameIdentifierClaim!.Value.Should().Be(user.Id);

            var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            emailClaim.Should().NotBeNull();
            emailClaim!.Value.Should().Be(user.Email);

            var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
            fullNameClaim.Should().NotBeNull();
            fullNameClaim!.Value.Should().Be(user.FullName);

            var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            roleClaims.Should().HaveCount(2);
            roleClaims.Should().Contain("User");
            roleClaims.Should().Contain("Admin");

            // Verify Expiration is close to 60 minutes from now (allowing delta for test run time)
            var expectedExpiration = DateTime.UtcNow.AddMinutes(60);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Verifies that GenerateToken throws an InvalidOperationException when the
        /// JWT Key configuration is missing.
        /// </summary>
        [Fact]
        public void GenerateToken_ShouldThrowInvalidOperationException_WhenJwtKeyIsMissing()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                // Missing "Jwt:Key"
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:DurationInMinutes", "60" }
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var jwtService = new JWTService(config);

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                FullName = "Test User"
            };

            var roles = new List<string> { "User" };

            // Act
            Action act = () => jwtService.GenerateToken(user, roles);

            // Assert
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Jwt:Key is not configured");
        }

        #endregion
    }
}
