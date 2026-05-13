using _10Pearls_Web_Project.Server.Enums;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class TaskResponseDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AppTaskStatus Status { get; set; }
        public AppTaskPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
