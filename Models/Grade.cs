using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class Grade
    {
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        
        [Range(0, 100)]
        public int Points { get; set; }
        
        [Range(2, 6)]
        public int? GradeValue { get; set; } // 2-6 скала
        
        [StringLength(500)]
        public string? Comments { get; set; }
        
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
        
        public string? GradedBy { get; set; } // Teacher ID
    }
} 