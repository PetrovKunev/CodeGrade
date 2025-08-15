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
                _logger.LogInformation("Updating grade: Id={Id}, Points={Points}, GradeValue={GradeValue}", 
                    existingGrade.Id, gradeUpdate.Points, gradeUpdate.GradeValue);

                existingGrade.Points = gradeUpdate.Points;
                existingGrade.GradeValue = gradeUpdate.GradeValue;
                existingGrade.Comments = gradeUpdate.Comments;
                existingGrade.GradedAt = DateTime.UtcNow;

                _context.Update(existingGrade);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Grade updated successfully");

                TempData["SuccessMessage"] = "Оценката беше успешно обновена.";
                return RedirectToAction(nameof(Details), new { id = existingGrade.Id });
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
        else
        {
            // Log за дебъгване
            _logger.LogWarning("Edit Grade - ModelState.IsValid: {IsValid}", ModelState.IsValid);
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
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

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
            return NotFound();

        var submission = await _context.Submissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(s => s.Assignment)
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
            return NotFound();

        // Проверяваме дали преподавателят има достъп до тази задача
        if (submission.Assignment.TeacherId != teacher.Id)
            return Forbid();

        // Проверяваме дали вече има оценка за това решение
        var existingGrade = await _context.Grades
            .FirstOrDefaultAsync(g => g.StudentId == submission.StudentId && g.AssignmentId == submission.AssignmentId);

        if (existingGrade != null)
        {
            // Ако вече има оценка, пренасочваме към детайлите на оценката
            return RedirectToAction(nameof(Details), new { id = existingGrade.Id });
        }

        return View(submission);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int id, [Bind("StudentId,AssignmentId,Points,GradeValue,Comments")] Grade grade)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
            return NotFound();

        var submission = await _context.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
            return NotFound();

        // Проверяваме дали преподавателят има достъп до тази задача
        if (submission.Assignment.TeacherId != teacher.Id)
            return Forbid();

        // Проверяваме дали вече има оценка за това решение
        var existingGrade = await _context.Grades
            .FirstOrDefaultAsync(g => g.StudentId == submission.StudentId && g.AssignmentId == submission.AssignmentId);

        if (existingGrade != null)
        {
            // Ако вече има оценка, пренасочваме към детайлите на оценката
            return RedirectToAction(nameof(Details), new { id = existingGrade.Id });
        }

        // Log за дебъгване
        _logger.LogInformation("Create Grade - ModelState.IsValid: {IsValid}", ModelState.IsValid);
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                grade.GradedAt = DateTime.UtcNow;
                grade.GradedBy = teacher.Id.ToString();

                _logger.LogInformation("Creating grade: StudentId={StudentId}, AssignmentId={AssignmentId}, Points={Points}, GradeValue={GradeValue}", 
                    grade.StudentId, grade.AssignmentId, grade.Points, grade.GradeValue);

                _context.Add(grade);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Grade created successfully with ID: {GradeId}", grade.Id);

                TempData["SuccessMessage"] = "Оценката беше успешно създадена.";
                // Пренасочваме към детайлите на новосъздадената оценка
                return RedirectToAction(nameof(Details), new { id = grade.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Грешка при създаване на оценка");
                ModelState.AddModelError("", "Възникна грешка при създаването на оценката. Моля, опитайте отново.");
            }
        }

        // Ако има грешки, зареждаме данните отново за изгледа
        var submissionForView = await _context.Submissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(s => s.Assignment)
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.Id == id);

        return View(submissionForView);
    }

    private bool GradeExists(int id)
    {
        return _context.Grades.Any(e => e.Id == id);
    }
} 