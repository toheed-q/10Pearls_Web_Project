namespace _10Pearls_Web_Project.Server.DTOs
{
    public class UserSummaryDto
    {
        public string Id        { get; set; } = string.Empty;
        public string FullName  { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string Role      { get; set; } = string.Empty;
        public int    TaskCount { get; set; }
    }

    public class UserDetailsDto : UserSummaryDto
    {
        public string? UserName  { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UserRoleChangedPayload
    {
        public string UserId  { get; set; } = string.Empty;
        public string OldRole { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
    }
}
