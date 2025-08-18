using System.ComponentModel.DataAnnotations;

namespace CodeGrade.ViewModels
{
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Името е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Фамилията е задължителна")]
        [StringLength(50, ErrorMessage = "Фамилията не може да бъде по-дълга от 50 символа")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Имейлът е задължителен")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Потребителското име е задължително")]
        public string UserName { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        
        public List<string> Roles { get; set; } = new List<string>();
        
        public string? StudentId { get; set; }
        public string? TeacherId { get; set; }
        
        // Display properties
        public string FullName => $"{FirstName} {LastName}";
        public string RoleDisplay => string.Join(", ", Roles);
    }

    public class AdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Името е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Фамилията е задължителна")]
        [StringLength(50, ErrorMessage = "Фамилията не може да бъде по-дълга от 50 символа")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Имейлът е задължителен")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Потребителското име е задължително")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Паролата е задължителна")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да бъде поне 6 символа")]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Потвърждението на паролата е задължително")]
        [Compare("Password", ErrorMessage = "Паролите не съвпадат")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Ролята е задължителна")]
        public string Role { get; set; } = string.Empty;
        
        public List<string> AvailableRoles { get; set; } = new List<string> { "Student", "Teacher", "Admin" };
    }

    public class AdminUserEditViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Името е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Фамилията е задължителна")]
        [StringLength(50, ErrorMessage = "Фамилията не може да бъде по-дълга от 50 символа")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Имейлът е задължителен")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Потребителското име е задължително")]
        public string UserName { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public List<string> Roles { get; set; } = new List<string>();
        
        public List<string> AvailableRoles { get; set; } = new List<string> { "Student", "Teacher", "Admin" };
    }
}
