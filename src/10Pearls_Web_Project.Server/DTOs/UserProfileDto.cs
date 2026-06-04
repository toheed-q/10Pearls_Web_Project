using _10Pearls_Web_Project.Server.DTOs;

namespace _10Pearls_Web_Project.Server.DTOs
{
    public class UserProfileDto
    {
        public string Id       { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
        public ProfileTaskStatsDto TaskStats { get; set; } = new();
    }

    public class ProfileTaskStatsDto
    {
        public int Total     { get; set; }
        public int Completed { get; set; }
        public int Pending   { get; set; }
        public int InProgress { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? FullName        { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword     { get; set; }
    }
}
