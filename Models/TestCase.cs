using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class TestCase
    {
        public int Id { get; set; }
        
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        
        public string Input { get; set; } = string.Empty;
        
        [Required]
        public string ExpectedOutput { get; set; } = string.Empty;
        
        [Range(0, 100)]
        public int Points { get; set; } = 1;
        
        public bool IsHidden { get; set; } = false; // скрит от учениците
        
        [StringLength(200)]
        public string? Description { get; set; }
    }
} 