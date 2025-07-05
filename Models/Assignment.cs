using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public string ProblemStatement { get; set; } = string.Empty;
        
        public int SubjectModuleId { get; set; }
        public SubjectModule SubjectModule { get; set; } = null!;
        
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;
        
        public DateTime DueDate { get; set; }
        
        [Range(1, 100)]
        public int MaxPoints { get; set; } = 10;
        
        [Range(1, 300)]
        public int TimeLimit { get; set; } = 5; // секунди
        
        [Range(1, 1024)]
        public int MemoryLimit { get; set; } = 128; // MB
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
} 