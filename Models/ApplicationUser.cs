using Microsoft.AspNetCore.Identity;

namespace CodeGrade.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
    }
} 