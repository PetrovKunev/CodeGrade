using System.ComponentModel.DataAnnotations;

namespace CodeGrade.ViewModels
{
    public class AdminModuleViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Името на модула е задължително")]
        [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Описанието не може да бъде по-дълго от 500 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Семестърът е задължителен")]
        [Range(1, 8, ErrorMessage = "Семестърът трябва да бъде между 1 и 8")]
        public int Semester { get; set; }
        
        public int AssignmentCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public List<AssignmentViewModel> Assignments { get; set; } = new List<AssignmentViewModel>();
    }

    public class AdminModuleCreateViewModel
    {
        [Required(ErrorMessage = "Името на модула е задължително")]
        [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Описанието не може да бъде по-дълго от 500 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Семестърът е задължителен")]
        [Range(1, 8, ErrorMessage = "Семестърът трябва да бъде между 1 и 8")]
        public int Semester { get; set; }
    }

    public class AdminModuleEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Името на модула е задължително")]
        [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Описанието не може да бъде по-дълго от 500 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Семестърът е задължителен")]
        [Range(1, 8, ErrorMessage = "Семестърът трябва да бъде между 1 и 8")]
        public int Semester { get; set; }
    }
}
