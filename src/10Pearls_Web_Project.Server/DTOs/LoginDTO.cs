using System.ComponentModel.DataAnnotations;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}