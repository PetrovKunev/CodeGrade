using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;
using CodeGrade.ViewModels;

namespace CodeGrade.Controllers;

[Authorize]
public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(ApplicationDbContext context, ILogger<AssignmentsController> logger)
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

            var assignments = await _context.Assignments
                .Include(a => a.SubjectModule)
                .Where(a => a.TeacherId == teacher.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View("TeacherAssignments", assignments);
        }
        else if (User.IsInRole("Student"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students
                .Include(s => s.ClassGroup)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            var assignments = await _context.Assignments
                .Include(a => a.SubjectModule)
                .Where(a => a.IsActive && a.DueDate > DateTime.UtcNow)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            return View("StudentAssignments", assignments);
        }

        return Forbid();
    }

    // GET: Assignments/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.SubjectModule)
            .Include(a => a.Teacher)
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            return NotFound();
        }

        // Check if user can view this assignment
        if (User.IsInRole("Teacher"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            
            if (teacher == null || assignment.TeacherId != teacher.Id)
            {
                return Forbid();
            }
            
            return View("TeacherDetails", assignment);
        }
        else if (User.IsInRole("Student"))
        {
            // Students can only view active assignments
            if (!assignment.IsActive)
            {
                return Forbid();
            }
            
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null)
            {
                return NotFound();
            }
            
            var publicTestCases = assignment.TestCases.Where(tc => !tc.IsHidden).ToList();
            var studentSubmissions = await _context.Submissions
                .Where(s => s.StudentId == student.Id && s.AssignmentId == assignment.Id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            
            var viewModel = new AssignmentDetailsViewModel
            {
                Assignment = assignment,
                PublicTestCases = publicTestCases,
                StudentSubmissions = studentSubmissions,
                SubmissionForm = new SubmissionViewModel
                {
                    AssignmentId = assignment.Id,
                    Assignment = assignment
                },
                CanSubmit = assignment.DueDate > DateTime.UtcNow,
                TimeRemaining = assignment.DueDate > DateTime.UtcNow 
                    ? $"{(assignment.DueDate - DateTime.UtcNow).Days} дни" 
                    : "Изтекъл"
            };
            
            return View("StudentDetails", viewModel);
        }

        return Forbid();
    }

    // GET: Assignments/Create
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var viewModel = new AssignmentViewModel
        {
            AvailableModules = await _context.SubjectModules.ToListAsync(),
            TestCases = new List<TestCaseViewModel>
            {
                new TestCaseViewModel { Points = 1 }
            }
        };

        return View(viewModel);
    }

    // POST: Assignments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(AssignmentViewModel model)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        // Reload available modules for dropdown
        model.AvailableModules = await _context.SubjectModules.ToListAsync();

        if (ModelState.IsValid)
        {
            var assignment = new Assignment
            {
                Title = model.Title,
                Description = model.Description,
                ProblemStatement = model.ProblemStatement,
                SubjectModuleId = model.SubjectModuleId,
                TeacherId = teacher.Id,
                DueDate = model.DueDate,
                MaxPoints = model.MaxPoints,
                TimeLimit = model.TimeLimit,
                MemoryLimit = model.MemoryLimit,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Add test cases
            foreach (var testCaseModel in model.TestCases.Where(tc => !string.IsNullOrEmpty(tc.Input)))
            {
                var testCase = new TestCase
                {
                    AssignmentId = assignment.Id,
                    Input = testCaseModel.Input,
                    ExpectedOutput = testCaseModel.ExpectedOutput,
                    Points = testCaseModel.Points,
                    IsHidden = testCaseModel.IsHidden
                };
                _context.TestCases.Add(testCase);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Задачата е създадена успешно!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Assignments/Edit/5
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);

        if (assignment == null)
        {
            return NotFound();
        }

        var viewModel = new AssignmentViewModel
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            ProblemStatement = assignment.ProblemStatement,
            SubjectModuleId = assignment.SubjectModuleId,
            DueDate = assignment.DueDate,
            MaxPoints = assignment.MaxPoints,
            TimeLimit = assignment.TimeLimit,
            MemoryLimit = assignment.MemoryLimit,
            IsActive = assignment.IsActive,
            AvailableModules = await _context.SubjectModules.ToListAsync(),
            TestCases = assignment.TestCases.Select(tc => new TestCaseViewModel
            {
                Id = tc.Id,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                Points = tc.Points,
                IsHidden = tc.IsHidden
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Assignments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id, AssignmentViewModel model)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);

        if (assignment == null)
        {
            return NotFound();
        }

        // Reload available modules for dropdown
        model.AvailableModules = await _context.SubjectModules.ToListAsync();

        if (ModelState.IsValid)
        {
            assignment.Title = model.Title;
            assignment.Description = model.Description;
            assignment.ProblemStatement = model.ProblemStatement;
            assignment.SubjectModuleId = model.SubjectModuleId;
            assignment.DueDate = model.DueDate;
            assignment.MaxPoints = model.MaxPoints;
            assignment.TimeLimit = model.TimeLimit;
            assignment.MemoryLimit = model.MemoryLimit;
            assignment.IsActive = model.IsActive;

            // Update test cases
            _context.TestCases.RemoveRange(assignment.TestCases);
            
            foreach (var testCaseModel in model.TestCases.Where(tc => !string.IsNullOrEmpty(tc.Input)))
            {
                var testCase = new TestCase
                {
                    AssignmentId = assignment.Id,
                    Input = testCaseModel.Input,
                    ExpectedOutput = testCaseModel.ExpectedOutput,
                    Points = testCaseModel.Points,
                    IsHidden = testCaseModel.IsHidden
                };
                _context.TestCases.Add(testCase);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Задачата е обновена успешно!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Assignments/Delete/5
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .Include(a => a.SubjectModule)
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);

        if (assignment == null)
        {
            return NotFound();
        }

        return View(assignment);
    }

    // POST: Assignments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);

        if (assignment != null)
        {
            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Задачата е изтрита успешно!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Assignments/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(SubmissionViewModel model)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId);

        if (assignment == null)
        {
            return NotFound();
        }

        // Check if assignment is still active and not past due date
        if (!assignment.IsActive || assignment.DueDate <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Задачата е неактивна или крайният срок е изтекъл.";
            return RedirectToAction("Details", new { id = model.AssignmentId });
        }

        if (ModelState.IsValid)
        {
            var submission = new Submission
            {
                StudentId = student.Id,
                AssignmentId = assignment.Id,
                Code = model.Code,
                Language = model.Language,
                SubmittedAt = DateTime.UtcNow,
                Status = SubmissionStatus.Pending
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // TODO: Queue for execution with Docker service
            // For now, just mark as submitted
            TempData["SuccessMessage"] = "Решението е подадено успешно! Обработването ще започне скоро.";
            
            return RedirectToAction("Details", new { id = model.AssignmentId });
        }

        TempData["ErrorMessage"] = "Грешка при подаването на решението.";
        return RedirectToAction("Details", new { id = model.AssignmentId });
    }
} 