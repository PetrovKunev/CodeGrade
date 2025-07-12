using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class QualityMetrics
    {
        public int Id { get; set; }
        
        public int SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        
        // Code complexity metrics
        public int CyclomaticComplexity { get; set; }
        public int LinesOfCode { get; set; }
        public int MethodCount { get; set; }
        public int ClassCount { get; set; }
        
        // Readability metrics
        [Range(0, 100)]
        public int ReadabilityScore { get; set; }
        
        [Range(0, 100)]
        public int NamingScore { get; set; }
        
        [Range(0, 100)]
        public int StructureScore { get; set; }
        
        // Documentation metrics
        [Range(0, 100)]
        public int DocumentationCoverage { get; set; }
        
        [Range(0, 100)]
        public int CommentQuality { get; set; }
        
        // Style compliance
        [Range(0, 100)]
        public int StyleScore { get; set; }
        
        public int StyleViolations { get; set; }
        
        // Maintainability metrics
        [Range(0, 100)]
        public int MaintainabilityIndex { get; set; }
        
        public double TechnicalDebt { get; set; }
        
        // Security considerations
        public int SecurityIssues { get; set; }
        public string? SecurityFeedback { get; set; }
        
        // Performance indicators
        public int PerformanceScore { get; set; }
        public string? PerformanceFeedback { get; set; }
        
        // Detailed feedback
        public string? QualityFeedback { get; set; }
        public string? ImprovementSuggestions { get; set; }
        
        // Analysis metadata
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string? AnalysisVersion { get; set; }
        public string? ToolsUsed { get; set; }
    }
}