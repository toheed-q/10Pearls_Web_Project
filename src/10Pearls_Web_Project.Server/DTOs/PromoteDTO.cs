using System.ComponentModel.DataAnnotations;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class PromoteDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
