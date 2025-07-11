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
        public string? CompilerOutput { get; set; }
        public string? RuntimeOutput { get; set; }
        
        public ExecutionStatus Status { get; set; }
        
        // Enhanced feedback properties
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? DetailedErrorType { get; set; }
        public int? LineNumber { get; set; }
        public string? StackTrace { get; set; }
        
        // Comparison details
        public bool? OutputTrimMatches { get; set; }
        public bool? OutputCaseInsensitiveMatches { get; set; }
        public string? DiffOutput { get; set; } // Formatted diff between expected and actual
    }
    
    public enum ExecutionStatus
    {
        Pending,
        Queued,
        Compiling,
        Running,
        Passed,
        Failed,
        PartiallyCorrect,
        TimeLimitExceeded,
        MemoryLimitExceeded,
        RuntimeError,
        CompilationError,
        OutputFormatError,
        SystemError
    }
} 