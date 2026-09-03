using Microsoft.AspNetCore.Identity;

namespace DevTasks.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
    }
}