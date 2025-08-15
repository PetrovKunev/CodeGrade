using System.ComponentModel.DataAnnotations;

namespace CodeGrade.ViewModels
{
    public class GradeEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Точките са задължителни")]
        [Range(0, 100, ErrorMessage = "Точките трябва да са между 0 и 100")]
        public int Points { get; set; }

        [Required(ErrorMessage = "Оценката е задължителна")]
        [Range(2.0, 6.0, ErrorMessage = "Оценката трябва да е между 2.0 и 6.0")]
        public decimal? GradeValue { get; set; }

        [StringLength(500, ErrorMessage = "Коментарът не може да е по-дълъг от 500 символа")]
        public string? Comments { get; set; }

        // За показване в изгледа
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
    }
}
