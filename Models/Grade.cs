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
        
        [Required]
        [Range(0, 100)]
        public int Points { get; set; }
        
        [Range(2.0, 6.0, ErrorMessage = "Оценката трябва да е между 2.0 и 6.0")]
        public decimal? GradeValue { get; set; } // 2-6 скала с десетични стойности
        
        [StringLength(500)]
        public string? Comments { get; set; }
        
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
        
        public string? GradedBy { get; set; } // Teacher ID
    }
} 