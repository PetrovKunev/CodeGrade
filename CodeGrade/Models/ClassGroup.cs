using System.ComponentModel.DataAnnotations;

namespace CodeGrade.Models
{
    public class ClassGroup
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        public int Year { get; set; }
        
        // Navigation properties
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
} 