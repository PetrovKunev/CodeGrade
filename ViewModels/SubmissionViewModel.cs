using System.ComponentModel.DataAnnotations;
using CodeGrade.Models;

namespace CodeGrade.ViewModels
{
    public class SubmissionViewModel
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        
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
        public Assignment Assignment { get; set; } = null!;
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
}