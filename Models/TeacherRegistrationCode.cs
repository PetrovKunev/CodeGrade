using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class TeacherRegistrationCode
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public string? UsedByUserId { get; set; }
        
        public DateTime? UsedAt { get; set; }
        
        public bool IsActive => !IsUsed && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
        
        // Navigation property
        public ApplicationUser? UsedByUser { get; set; }
    }
}
