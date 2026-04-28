using Microsoft.AspNetCore.Identity;

namespace _10Pearls_Web_Project.Server.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; }



    }
}
