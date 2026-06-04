using System.Text.Json.Serialization;
using _10Pearls_Web_Project.Server.Enums;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class TaskResponseDTO
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AppTaskStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AppTaskPriority Priority { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string UserId { get; set; } = string.Empty;

        // Full name of the task owner — populated from AspNetUsers join
        public string OwnerName { get; set; } = string.Empty;
    }
}
