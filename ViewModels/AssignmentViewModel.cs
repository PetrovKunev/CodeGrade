using System.ComponentModel.DataAnnotations;
using CodeGrade.Models;

namespace CodeGrade.ViewModels
{
    public class AssignmentViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Заглавието е задължително")]
        [StringLength(200, ErrorMessage = "Заглавието не може да бъде по-дълго от 200 символа")]
        [Display(Name = "Заглавие")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Описанието не може да бъде по-дълго от 1000 символа")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Условието на задачата е задължително")]
        [Display(Name = "Условие на задачата")]
        public string ProblemStatement { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Модулът е задължителен")]
        [Display(Name = "Модул")]
        public int SubjectModuleId { get; set; }
        
        [Required(ErrorMessage = "Крайният срок е задължителен")]
        [Display(Name = "Краен срок")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
        
        [Range(1, 100, ErrorMessage = "Максималните точки трябва да са между 1 и 100")]
        [Display(Name = "Максимални точки")]
        public int MaxPoints { get; set; } = 10;
        
        [Range(1, 300, ErrorMessage = "Времевият лимит трябва да е между 1 и 300 секунди")]
        [Display(Name = "Времеви лимит (секунди)")]
        public int TimeLimit { get; set; } = 5;
        
        [Range(1, 1024, ErrorMessage = "Лимитът на паметта трябва да е между 1 и 1024 MB")]
        [Display(Name = "Лимит на паметта (MB)")]
        public int MemoryLimit { get; set; } = 128;
        
        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;
        
        [Required(ErrorMessage = "Езикът е задължителен")]
        [Display(Name = "Език на задачата")]
        public string Language { get; set; } = "csharp";

        public List<(string Value, string Display)> AvailableLanguages { get; set; } = new List<(string, string)>
        {
            ("csharp", "C# (.NET Core)"),
            ("python", "Python 3.8.1"),
            ("java", "Java (OpenJDK 13)"),
            ("javascript", "JavaScript (Node.js)"),
            ("cpp", "C++ (GCC 9.2.0)"),
            ("php", "PHP 7.4.1"),
            ("ruby", "Ruby 2.7.0"),
            ("go", "Go 1.13.5"),
            ("rust", "Rust 1.40.0")
        };
        
        // For dropdown population
        public List<SubjectModule> AvailableModules { get; set; } = new List<SubjectModule>();
        
        // Test cases
        public List<TestCaseViewModel> TestCases { get; set; } = new List<TestCaseViewModel>();

        [Display(Name = "Клас")]
        public int? ClassGroupId { get; set; }
        public List<ClassGroup> AvailableClassGroups { get; set; } = new List<ClassGroup>();
    }
    
    public class TestCaseViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Вход")]
        public string Input { get; set; } = string.Empty;
        
        [Display(Name = "Очакван изход")]
        public string ExpectedOutput { get; set; } = string.Empty;
        
        [Range(0, 100, ErrorMessage = "Точките трябва да са между 0 и 100")]
        [Display(Name = "Точки")]
        public int Points { get; set; } = 1;
        
        [Display(Name = "Скрит тест")]
        public bool IsHidden { get; set; } = false;
    }
}