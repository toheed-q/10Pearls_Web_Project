using _10Pearls_Web_Project.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class CreateTaskDTO
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        public AppTaskStatus Status { get; set; } = AppTaskStatus.Pending;

        public AppTaskPriority Priority { get; set; } = AppTaskPriority.Medium;
    }
}
