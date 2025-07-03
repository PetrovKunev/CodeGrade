using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class Submission
    {
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        
        [Required]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Language { get; set; } = string.Empty; // "csharp", "python", etc.
        
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        
        [Range(0, 100)]
        public int Score { get; set; } = 0;
        
        public string? CompilerOutput { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public int? ExecutionTime { get; set; } // milliseconds
        public int? MemoryUsed { get; set; } // KB
        
        // Navigation properties
        public ICollection<ExecutionResult> ExecutionResults { get; set; } = new List<ExecutionResult>();
    }
    
    public enum SubmissionStatus
    {
        Pending,
        Compiling,
        Running,
        Completed,
        CompilationError,
        RuntimeError,
        TimeLimitExceeded,
        MemoryLimitExceeded
    }
} 