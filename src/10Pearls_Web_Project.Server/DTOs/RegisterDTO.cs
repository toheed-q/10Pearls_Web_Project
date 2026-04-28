using System.ComponentModel.DataAnnotations;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class RegisterDTO
    {
        [Required, MinLength(2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
