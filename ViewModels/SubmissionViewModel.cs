using System.ComponentModel.DataAnnotations;
using CodeGrade.Models;

namespace CodeGrade.ViewModels
{
    public class SubmissionViewModel
    {
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }
        
        [Required(ErrorMessage = "Кодът е задължителен")]
        [Display(Name = "Код")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Езикът е задължителен")]
        [Display(Name = "Език")]
        public string Language { get; set; } = "csharp";
        
        public List<string> AvailableLanguages { get; set; } = new List<string>
        {
            "csharp", "python", "java", "javascript"
        };
        
        public List<ExecutionResult> PreviousResults { get; set; } = new List<ExecutionResult>();
        public List<Submission> PreviousSubmissions { get; set; } = new List<Submission>();
    }
    
    public class AssignmentDetailsViewModel
    {
        public Assignment? Assignment { get; set; }
        public List<TestCase> PublicTestCases { get; set; } = new List<TestCase>();
        public List<Submission> StudentSubmissions { get; set; } = new List<Submission>();
        public SubmissionViewModel SubmissionForm { get; set; } = new SubmissionViewModel();
        public bool CanSubmit { get; set; } = true;
        public string TimeRemaining { get; set; } = string.Empty;
    }
    
    public class TestCodeRequest
    {
        [Required]
        public int AssignmentId { get; set; }
        
        [Required]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        public string Language { get; set; } = string.Empty;
    }

    // New ViewModels for submission details
    public class SubmissionDetailsViewModel
    {
        public int Id { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public string SubjectModuleName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string ClassGroupName { get; set; } = string.Empty;
        public int ClassNumber { get; set; }
        public string SubGroup { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; }
        public string Language { get; set; } = string.Empty;
        public string LanguageDisplay { get; set; } = string.Empty;
        public int? ExecutionTime { get; set; }
        public int? MemoryUsed { get; set; }
        public int Score { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string CompilerOutput { get; set; } = string.Empty;
        public List<ExecutionResultViewModel> ExecutionResults { get; set; } = new List<ExecutionResultViewModel>();
        
        // Assignment details
        public DateTime? DueDate { get; set; }
        public int MaxPoints { get; set; }
        public int TimeLimit { get; set; }
        public int MemoryLimit { get; set; }
        public bool IsDueDatePassed { get; set; }
        public bool CanSubmitNewSolution { get; set; }
        
        // Teacher-specific properties
        public bool IsTeacherView { get; set; }
        public bool HasGrade { get; set; }
    }

    public class ExecutionResultViewModel
    {
        public int TestCaseId { get; set; }
        public bool IsCorrect { get; set; }
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string ActualOutput { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}