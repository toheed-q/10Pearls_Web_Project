using _10Pearls_Web_Project.Server.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _10Pearls_Web_Project.Server.Services
{
    public class JWTService
    {
        private readonly IConfiguration _config;

        public JWTService(IConfiguration config)
        {
            _config = config;
        }

        public virtual string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _config.GetSection("Jwt");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured"))
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                // Standard identity claims — read by ClaimTypes.NameIdentifier, .Email, .Name
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email,          user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name,           user.FullName ?? string.Empty),
            };

            // Role claims — read by ClaimTypes.Role and User.IsInRole()
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var token = new JwtSecurityToken(
                issuer:            jwtSettings["Issuer"],
                audience:          jwtSettings["Audience"],
                claims:            claims,
                expires:           DateTime.UtcNow.AddMinutes(
                                       Convert.ToDouble(jwtSettings["DurationInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
