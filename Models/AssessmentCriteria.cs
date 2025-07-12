using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class AssessmentCriteria
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // Difficulty level affects weighting
        public DifficultyLevel Level { get; set; }
        
        // Weighting percentages (must sum to 100)
        [Range(0, 100)]
        public int CorrectnessWeight { get; set; } = 50;
        
        [Range(0, 100)]
        public int QualityWeight { get; set; } = 30;
        
        [Range(0, 100)]
        public int TestingWeight { get; set; } = 15;
        
        [Range(0, 100)]
        public int ApproachWeight { get; set; } = 5;
        
        // Quality thresholds for different difficulty levels
        public int MinReadabilityScore { get; set; } = 60;
        public int MinMaintainabilityScore { get; set; } = 60;
        public int MinDocumentationScore { get; set; } = 40;
        
        // Pass/fail thresholds
        [Range(0, 100)]
        public int PassThreshold { get; set; } = 50;
        
        [Range(0, 100)]
        public int ExcellenceThreshold { get; set; } = 90;
        
        // Unlimited resubmissions
        public bool AllowUnlimitedResubmissions { get; set; } = true;
        public bool TrackImprovement { get; set; } = true;
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<GradeResult> GradeResults { get; set; } = new List<GradeResult>();
    }
    
    public enum DifficultyLevel
    {
        Beginner = 1,
        Intermediate = 2,
        Advanced = 3
    }
}