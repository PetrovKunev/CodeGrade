using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;
using CodeGrade.Services;

namespace CodeGrade.Controllers;

[Authorize]
public class GradesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly GradeCalculationService _gradeService;
    private readonly ILogger<GradesController> _logger;

    public GradesController(ApplicationDbContext context, GradeCalculationService gradeService, ILogger<GradesController> logger)
    {
        _context = context;
        _gradeService = gradeService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Teacher"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return NotFound();

            var grades = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Assignment)
                .Where(g => g.Assignment.TeacherId == teacher.Id)
                .OrderByDescending(g => g.GradedAt)
                .ToListAsync();

            return View("TeacherGrades", grades);
        }

        return Forbid();
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyGrades()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
            return NotFound();

        var grades = await _context.Grades
            .Include(g => g.Assignment)
            .Where(g => g.StudentId == student.Id)
            .OrderByDescending(g => g.GradedAt)
            .ToListAsync();

        var statistics = await _gradeService.GetStudentStatisticsAsync(student.Id);

        ViewBag.Statistics = statistics;

        return View("StudentGrades", grades);
    }
} 