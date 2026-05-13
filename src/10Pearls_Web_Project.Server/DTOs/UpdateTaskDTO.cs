using _10Pearls_Web_Project.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class UpdateTaskDTO
    {
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public AppTaskStatus? Status { get; set; }

        public AppTaskPriority? Priority { get; set; }
    }
}
