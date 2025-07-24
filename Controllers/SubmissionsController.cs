using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;
using CodeGrade.ViewModels;

namespace CodeGrade.Controllers;

[Authorize]
public class SubmissionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(ApplicationDbContext context, ILogger<SubmissionsController> logger)
    {
        _context = context;
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

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .Include(s => s.Student)
                    .ThenInclude(st => st.ClassGroup)
                .Include(s => s.Assignment)
                .Where(s => s.Assignment.TeacherId == teacher.Id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View("TeacherSubmissions", submissions);
        }

        return Forbid();
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MySubmissions()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
            return NotFound();

        var submissions = await _context.Submissions
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return View("MySubmissions", submissions);
    }

    [Authorize(Roles = "Student")]
    [Route("Submissions/AssignmentSubmissions/{assignmentId}")]
    public async Task<IActionResult> AssignmentSubmissions(int assignmentId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
            return NotFound();

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return NotFound();

        var submissions = await _context.Submissions
            .Include(s => s.ExecutionResults)
            .ThenInclude(er => er.TestCase)
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id && s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        ViewBag.Assignment = assignment;
        ViewBag.SubmissionsCount = submissions.Count;
        ViewBag.MaxSubmissions = 3;

        return View("AssignmentSubmissions", submissions);
    }

    // GET: Submissions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var submission = await _context.Submissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(s => s.Assignment)
                .ThenInclude(a => a.SubjectModule)
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        // Create the view model
        var viewModel = new SubmissionDetailsViewModel
        {
            Id = submission.Id,
            AssignmentTitle = submission.Assignment?.Title ?? "No assignment",
            SubjectModuleName = submission.Assignment?.SubjectModule?.Name ?? "-",
            StudentName = submission.Student?.User != null 
                ? $"{submission.Student.User.FirstName} {submission.Student.User.LastName}"
                : submission.Student?.StudentNumber ?? "Unknown",
            StudentNumber = submission.Student?.StudentNumber ?? "Unknown",
            ClassGroupName = submission.Student?.ClassGroup?.Name ?? "-",
            ClassNumber = submission.Student?.ClassNumber ?? 0,
            SubGroup = submission.Student?.SubGroup ?? "",
            SubmittedAt = submission.SubmittedAt,
            Status = submission.Status,
            Language = submission.Language ?? "",
            LanguageDisplay = GetLanguageDisplay(submission.Language ?? ""),
            ExecutionTime = submission.ExecutionTime,
            MemoryUsed = submission.MemoryUsed,
            Score = submission.Score,
            Code = submission.Code ?? "No code submitted",
            ErrorMessage = submission.ErrorMessage ?? "",
            CompilerOutput = submission.CompilerOutput ?? "",
            DueDate = submission.Assignment?.DueDate,
            MaxPoints = submission.Assignment?.MaxPoints ?? 0,
            TimeLimit = submission.Assignment?.TimeLimit ?? 0,
            MemoryLimit = submission.Assignment?.MemoryLimit ?? 0,
            IsDueDatePassed = submission.Assignment != null && submission.Assignment.DueDate <= DateTime.UtcNow,
            CanSubmitNewSolution = submission.Assignment != null && submission.Assignment.DueDate > DateTime.UtcNow,
            HasGrade = submission.Score > 0,
            ExecutionResults = submission.ExecutionResults?.Select(er => new ExecutionResultViewModel
            {
                TestCaseId = er.TestCaseId ?? 0,
                IsCorrect = er.IsCorrect,
                Input = er.Input ?? er.TestCase?.Input ?? "-",
                ExpectedOutput = er.ExpectedOutput ?? "-",
                ActualOutput = er.ActualOutput ?? "No output",
                ErrorMessage = er.ErrorMessage ?? "",
                PointsEarned = er.PointsEarned
            }).ToList() ?? new List<ExecutionResultViewModel>()
        };

        // Check if user can view this submission
        if (User.IsInRole("Teacher"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            
            if (teacher == null || submission.Assignment?.TeacherId != teacher.Id)
            {
                return Forbid();
            }
            
            viewModel.IsTeacherView = true;
            return View("TeacherSubmissionDetails", viewModel);
        }
        else if (User.IsInRole("Student"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null || submission.StudentId != student.Id)
            {
                return Forbid();
            }
            
            viewModel.IsTeacherView = false;
            return View("StudentSubmissionDetails", viewModel);
        }

        return Forbid();
    }

    // GET: Submissions/Delete/5
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var submission = await _context.Submissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Student)
                .ThenInclude(st => st.ClassGroup)
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        // Check if the teacher owns the assignment
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
        
        if (teacher == null || submission.Assignment?.TeacherId != teacher.Id)
        {
            return Forbid();
        }

        var viewModel = new SubmissionDetailsViewModel
        {
            Id = submission.Id,
            AssignmentTitle = submission.Assignment?.Title ?? "No assignment",
            StudentName = submission.Student?.User != null 
                ? $"{submission.Student.User.FirstName} {submission.Student.User.LastName}"
                : submission.Student?.StudentNumber ?? "Unknown",
            StudentNumber = submission.Student?.StudentNumber ?? "Unknown",
            ClassGroupName = submission.Student?.ClassGroup?.Name ?? "-",
            ClassNumber = submission.Student?.ClassNumber ?? 0,
            SubGroup = submission.Student?.SubGroup ?? "",
            SubmittedAt = submission.SubmittedAt,
            Status = submission.Status,
            Language = submission.Language ?? "",
            LanguageDisplay = GetLanguageDisplay(submission.Language ?? ""),
            Score = submission.Score,
            Code = submission.Code ?? "No code submitted"
        };

        return View("Delete", viewModel);
    }

    // POST: Submissions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var submission = await _context.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.ExecutionResults)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        // Check if the teacher owns the assignment
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
        
        if (teacher == null || submission.Assignment?.TeacherId != teacher.Id)
        {
            return Forbid();
        }

        try
        {
            // Remove execution results first (due to foreign key constraints)
            _context.ExecutionResults.RemoveRange(submission.ExecutionResults);
            
            // Remove the submission
            _context.Submissions.Remove(submission);
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Решението беше успешно изтрито.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting submission {SubmissionId}", id);
            TempData["ErrorMessage"] = "Възникна грешка при изтриването на решението.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private string GetLanguageDisplay(string language)
    {
        return language switch
        {
            "csharp" => "C#",
            "python" => "Python",
            "java" => "Java",
            "javascript" => "JavaScript",
            _ => language
        };
    }
} 