using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Data;
using CodeGrade.Models;
using CodeGrade.ViewModels;

namespace CodeGrade.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var teacherCodes = await _context.TeacherRegistrationCodes
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(teacherCodes);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateTeacherCode(string description, int? expiresInDays)
        {
            try
            {
                var code = GenerateUniqueCode();
                var expiresAt = expiresInDays.HasValue ? DateTime.UtcNow.AddDays(expiresInDays.Value) : (DateTime?)null;

                var teacherCode = new TeacherRegistrationCode
                {
                    Code = code,
                    Description = description ?? "Код за регистрация на преподавател",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _context.TeacherRegistrationCodes.Add(teacherCode);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated teacher registration code: {Code} by admin {AdminId}", 
                    code, User.Identity?.Name);

                TempData["SuccessMessage"] = $"Кодът '{code}' е генериран успешно!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating teacher registration code");
                TempData["ErrorMessage"] = "Грешка при генериране на кода.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeacherCode(int id)
        {
            try
            {
                var code = await _context.TeacherRegistrationCodes.FindAsync(id);
                if (code != null)
                {
                    _context.TeacherRegistrationCodes.Remove(code);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted teacher registration code: {Code} by admin {AdminId}", 
                        code.Code, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Кодът е изтрит успешно!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting teacher registration code {Id}", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на кода.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            
            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.TeacherRegistrationCodes.Any(c => c.Code == code));

            return code;
        }
    }
} 