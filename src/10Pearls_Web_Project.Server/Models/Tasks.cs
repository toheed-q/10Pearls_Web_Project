namespace _10Pearls_Web_Project.Server.Models
{
    public class Tasks
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public string Status { get; set; }   
        public string Priority { get; set; } 

        public DateTime DueDate { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
