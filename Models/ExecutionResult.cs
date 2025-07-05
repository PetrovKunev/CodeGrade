using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class ExecutionResult
    {
        public int Id { get; set; }
        
        public int SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        
        public int TestCaseId { get; set; }
        public TestCase TestCase { get; set; } = null!;
        
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string? ActualOutput { get; set; }
        
        public bool IsCorrect { get; set; }
        
        public int PointsEarned { get; set; }
        
        public int? ExecutionTime { get; set; } // milliseconds
        public int? MemoryUsed { get; set; } // KB
        
        public string? ErrorMessage { get; set; }
        
        public ExecutionStatus Status { get; set; }
    }
    
    public enum ExecutionStatus
    {
        Pending,
        Running,
        Passed,
        Failed,
        TimeLimitExceeded,
        MemoryLimitExceeded,
        RuntimeError,
        CompilationError
    }
} 