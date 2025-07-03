using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        [StringLength(100)]
        public string? Department { get; set; }
        
        [StringLength(50)]
        public string? Title { get; set; } // Dr., Prof., etc.
        
        // Navigation properties
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
} 