using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class Student
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        [Required]
        [StringLength(20)]
        public string StudentNumber { get; set; } = string.Empty;
        
        public int? ClassGroupId { get; set; }
        public ClassGroup? ClassGroup { get; set; }
        
        [StringLength(50)]
        public string? SubGroup { get; set; }
        
        // Navigation properties
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
} 