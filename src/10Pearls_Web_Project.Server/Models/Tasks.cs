using _10Pearls_Web_Project.Server.Enums;

namespace _10Pearls_Web_Project.Server.Models
{
    public class Tasks
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public AppTaskStatus Status { get; set; } = AppTaskStatus.Pending;

        public AppTaskPriority Priority { get; set; } = AppTaskPriority.Medium;

        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public string UserId { get; set; } = string.Empty;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }
}
