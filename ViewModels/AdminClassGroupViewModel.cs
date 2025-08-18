using System.ComponentModel.DataAnnotations;

namespace CodeGrade.ViewModels
{
    public class AdminClassGroupViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Името на класа е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "Описанието не може да бъде по-дълго от 200 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Годината е задължителна")]
        [Range(2020, 2030, ErrorMessage = "Годината трябва да бъде между 2020 и 2030")]
        public int Year { get; set; }
        
        public int StudentCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public List<StudentViewModel> Students { get; set; } = new List<StudentViewModel>();
    }

    public class AdminClassGroupCreateViewModel
    {
        [Required(ErrorMessage = "Името на класа е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "Описанието не може да бъде по-дълго от 200 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Годината е задължителна")]
        [Range(2020, 2030, ErrorMessage = "Годината трябва да бъде между 2020 и 2030")]
        public int Year { get; set; }
    }

    public class AdminClassGroupEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Името на класа е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "Описанието не може да бъде по-дълго от 200 символа")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Годината е задължителна")]
        [Range(2020, 2030, ErrorMessage = "Годината трябва да бъде между 2020 и 2030")]
        public int Year { get; set; }
    }

    public class StudentViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
    }
}
