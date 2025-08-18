using System.ComponentModel.DataAnnotations;

namespace CodeGrade.ViewModels
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Името е задължително")]
        [StringLength(50, ErrorMessage = "Името не може да бъде по-дълго от 50 символа")]
        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилията е задължителна")]
        [StringLength(50, ErrorMessage = "Фамилията не може да бъде по-дълга от 50 символа")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейл адресът е задължителен")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес")]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Паролата е задължителна")]
        [StringLength(100, ErrorMessage = "Паролата трябва да бъде поне {2} символа", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Потвърди парола")]
        [Compare("Password", ErrorMessage = "Паролите не съвпадат")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Моля изберете роля")]
        [Display(Name = "Роля")]
        public string Role { get; set; } = "Student";

        // Нови полета за студент
        [Display(Name = "Клас")]
        public int? ClassGroupId { get; set; }

        [Required(ErrorMessage = "Групата е задължителна за студенти")]
        [Display(Name = "Група")]
        public string SubGroup { get; set; } = string.Empty;

        [Range(1, 30, ErrorMessage = "Номерът в клас трябва да е между 1 и 30")]
        [Display(Name = "Номер в клас")]
        public int? ClassNumber { get; set; }

        [Display(Name = "Код за преподавател")]
        [StringLength(20, ErrorMessage = "Кодът не може да бъде по-дълъг от 20 символа")]
        public string? TeacherCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == "Student")
            {
                if (!ClassNumber.HasValue)
                {
                    yield return new ValidationResult("Номерът в клас е задължителен за студенти", new[] { nameof(ClassNumber) });
                }
                
                if (!ClassGroupId.HasValue)
                {
                    yield return new ValidationResult("Класът е задължителен за студенти", new[] { nameof(ClassGroupId) });
                }

                if (string.IsNullOrEmpty(SubGroup))
                {
                    yield return new ValidationResult("Групата е задължителна за студенти", new[] { nameof(SubGroup) });
                }
            }

            if (Role == "Teacher" && string.IsNullOrWhiteSpace(TeacherCode))
            {
                yield return new ValidationResult(
                    "Кодът за преподавател е задължителен за регистрация като преподавател",
                    new[] { nameof(TeacherCode) });
            }

            if (Role != "Teacher" && !string.IsNullOrWhiteSpace(TeacherCode))
            {
                yield return new ValidationResult(
                    "Кодът за преподавател се използва само за регистрация като преподавател",
                    new[] { nameof(TeacherCode) });
            }
        }
    }
} 