using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class SubjectModule
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int Semester { get; set; }
        
        // Navigation properties
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
} 