namespace _10Pearls_Web_Project.Server.DTOs
{
    public class TaskPagedResponseDTO
    {
        public List<TaskResponseDTO> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
