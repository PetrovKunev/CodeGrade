using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
                    .ThenInclude(st => st.User)
                .Include(g => g.Student)
                    .ThenInclude(st => st.ClassGroup)
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

        return View("MyGrades", grades);
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
            return NotFound();

        var grade = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(st => st.User)
            .Include(g => g.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(g => g.Assignment)
            .FirstOrDefaultAsync(g => g.Id == id && g.Assignment.TeacherId == teacher.Id);

        if (grade == null)
            return NotFound();

        // Зареждаме submission отделно
        var submission = await _context.Submissions
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.StudentId == grade.StudentId && s.AssignmentId == grade.AssignmentId);

        ViewBag.Submission = submission;

        if (grade == null)
            return NotFound();

        return View("GradeDetails", grade);
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
            return NotFound();

        var grade = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(st => st.User)
            .Include(g => g.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(g => g.Assignment)
            .FirstOrDefaultAsync(g => g.Id == id && g.Assignment.TeacherId == teacher.Id);

        if (grade == null)
            return NotFound();

        // Зареждаме submission отделно
        var submission = await _context.Submissions
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.StudentId == grade.StudentId && s.AssignmentId == grade.AssignmentId);

        ViewBag.Submission = submission;

        if (grade == null)
            return NotFound();

        return View("GradeEdit", grade);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Points,GradeValue,Comments")] Grade gradeUpdate)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
            return NotFound();

        var existingGrade = await _context.Grades
            .Include(g => g.Assignment)
            .FirstOrDefaultAsync(g => g.Id == id && g.Assignment.TeacherId == teacher.Id);

        if (existingGrade == null)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                existingGrade.Points = gradeUpdate.Points;
                existingGrade.GradeValue = gradeUpdate.GradeValue;
                existingGrade.Comments = gradeUpdate.Comments;
                existingGrade.GradedAt = DateTime.UtcNow;

                _context.Update(existingGrade);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Оценката беше успешно обновена.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GradeExists(existingGrade.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // Ако има грешки, зареждаме данните отново за изгледа
        var grade = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(st => st.User)
            .Include(g => g.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(g => g.Assignment)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade != null)
        {
            // Зареждаме submission отделно
            var submission = await _context.Submissions
                .Include(s => s.ExecutionResults)
                    .ThenInclude(er => er.TestCase)
                .FirstOrDefaultAsync(s => s.StudentId == grade.StudentId && s.AssignmentId == grade.AssignmentId);

            ViewBag.Submission = submission;
        }

        return View("GradeEdit", grade);
    }

    private bool GradeExists(int id)
    {
        return _context.Grades.Any(e => e.Id == id);
    }
} 