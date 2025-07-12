using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class GradeResult
    {
        public int Id { get; set; }
        
        public int SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        
        // Multi-dimensional scoring
        [Range(0, 100)]
        public int CorrectnessScore { get; set; }
        
        [Range(0, 100)]
        public int QualityScore { get; set; }
        
        [Range(0, 100)]
        public int TestingScore { get; set; }
        
        [Range(0, 100)]
        public int ApproachScore { get; set; }
        
        // Weighted final score
        [Range(0, 100)]
        public int FinalScore { get; set; }
        
        // Traditional grade (2-6 scale)
        [Range(2, 6)]
        public int? GradeValue { get; set; }
        
        // Detailed feedback
        public string? CorrectnessFeedback { get; set; }
        public string? QualityFeedback { get; set; }
        public string? TestingFeedback { get; set; }
        public string? ApproachFeedback { get; set; }
        public string? OverallFeedback { get; set; }
        
        // Metrics
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public string? CalculatedBy { get; set; } // System or Teacher ID
        
        // Learning progress
        public int AttemptNumber { get; set; } = 1;
        public int? PreviousScore { get; set; }
        public bool IsImprovement { get; set; }
        
        // Assessment criteria used
        public int AssessmentCriteriaId { get; set; }
        public AssessmentCriteria AssessmentCriteria { get; set; } = null!;
    }
}