namespace _10Pearls_Web_Project.Server.DTOs
{
    public class TaskStatsDTO
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
    }
}
