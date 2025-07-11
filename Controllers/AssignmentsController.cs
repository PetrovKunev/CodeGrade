using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;
using CodeGrade.ViewModels;
using CodeGrade.Services;

namespace CodeGrade.Controllers;

[Authorize]
public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssignmentsController> _logger;
    private readonly ICodeExecutorService _codeExecutor;

    public AssignmentsController(ApplicationDbContext context, ILogger<AssignmentsController> logger, ICodeExecutorService codeExecutor)
    {
        _context = context;
        _logger = logger;
        _codeExecutor = codeExecutor;
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

            // Execute code against test cases
            try
            {
                submission.Status = SubmissionStatus.Running;
                await _context.SaveChangesAsync();

                var executionResults = await _codeExecutor.ExecuteAllTestCasesAsync(
                    submission.Code,
                    submission.Language,
                    assignment.TestCases.ToList(),
                    assignment.TimeLimit,
                    assignment.MemoryLimit
                );

                // Store execution results
                foreach (var result in executionResults)
                {
                    _context.ExecutionResults.Add(result);
                }

                // Calculate final grade
                var totalPoints = assignment.TestCases.Sum(tc => tc.Points);
                var earnedPoints = executionResults.Where(r => r.IsSuccess).Sum(r => r.TestCase.Points);
                var grade = new Grade
                {
                    SubmissionId = submission.Id,
                    Points = earnedPoints,
                    MaxPoints = totalPoints,
                    Percentage = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0,
                    GradedAt = DateTime.UtcNow
                };

                _context.Grades.Add(grade);
                submission.Status = SubmissionStatus.Completed;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Решението е подадено и оценено успешно! Резултат: {earnedPoints}/{totalPoints} точки.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing submission {SubmissionId}", submission.Id);
                submission.Status = SubmissionStatus.Error;
                await _context.SaveChangesAsync();
                
                TempData["ErrorMessage"] = "Възникна грешка при изпълнението на кода. Моля, опитайте отново.";
            }
            
            return RedirectToAction("Details", new { id = model.AssignmentId });
        }

        TempData["ErrorMessage"] = "Грешка при подаването на решението.";
        return RedirectToAction("Details", new { id = model.AssignmentId });
    }

    // POST: Assignments/TestCode
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> TestCode([FromBody] TestCodeRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

            var assignment = await _context.Assignments
                .Include(a => a.TestCases)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId);

            if (assignment == null)
            {
                return Json(new { success = false, error = "Assignment not found" });
            }

            // Get public test cases only
            var publicTestCases = assignment.TestCases.Where(tc => !tc.IsHidden).ToList();
            if (!publicTestCases.Any())
            {
                return Json(new { success = false, error = "No public test cases available" });
            }

            var results = new List<object>();

            foreach (var testCase in publicTestCases)
            {
                var result = await _codeExecutor.ExecuteCodeAsync(
                    request.Code,
                    request.Language,
                    testCase,
                    assignment.TimeLimit,
                    assignment.MemoryLimit
                );

                results.Add(new
                {
                    input = testCase.Input,
                    expectedOutput = testCase.ExpectedOutput,
                    actualOutput = result.Output,
                    success = result.IsSuccess,
                    executionTime = result.ExecutionTime,
                    status = result.Status.ToString(),
                    errorMessage = result.ErrorMessage
                });
            }

            return Json(new { success = true, results = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing code for assignment {AssignmentId}", request.AssignmentId);
            return Json(new { success = false, error = "An error occurred while testing your code" });
        }
    }

    // GET: Assignments/SubmissionStatus/5
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmissionStatus(int id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.ExecutionResults)
                    .ThenInclude(er => er.TestCase)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == student.Id);

            if (submission == null)
            {
                return Json(new { success = false, error = "Submission not found" });
            }

            var response = new
            {
                success = true,
                submissionId = submission.Id,
                status = submission.Status.ToString(),
                submittedAt = submission.SubmittedAt.ToString("dd.MM.yyyy HH:mm"),
                code = submission.Code,
                language = submission.Language,
                results = submission.ExecutionResults.Select(er => new
                {
                    testCaseId = er.TestCaseId,
                    input = er.TestCase.Input,
                    expectedOutput = er.TestCase.ExpectedOutput,
                    actualOutput = er.Output,
                    isSuccess = er.IsSuccess,
                    executionTime = er.ExecutionTime,
                    status = er.Status.ToString(),
                    errorMessage = er.ErrorMessage,
                    points = er.IsSuccess ? er.TestCase.Points : 0
                }).ToList(),
                grade = submission.Grade != null ? new
                {
                    points = submission.Grade.Points,
                    maxPoints = submission.Grade.MaxPoints,
                    percentage = submission.Grade.Percentage,
                    gradedAt = submission.Grade.GradedAt.ToString("dd.MM.yyyy HH:mm")
                } : null
            };

            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission status for submission {SubmissionId}", id);
            return Json(new { success = false, error = "An error occurred while getting submission status" });
        }
    }
} 