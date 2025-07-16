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
    private readonly GradeCalculationService _gradeCalculationService;

    public AssignmentsController(ApplicationDbContext context, ILogger<AssignmentsController> logger, ICodeExecutorService codeExecutor, GradeCalculationService gradeCalculationService)
    {
        _context = context;
        _logger = logger;
        _codeExecutor = codeExecutor;
        _gradeCalculationService = gradeCalculationService;
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
                .Include(a => a.ClassGroup)
                .Where(a => a.TeacherId == teacher.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Modules = await _context.SubjectModules.ToListAsync();
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
                .Include(a => a.ClassGroup)
                .Where(a => a.IsActive && a.DueDate > DateTime.UtcNow && a.ClassGroupId == student.ClassGroupId)
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
            .Include(a => a.ClassGroup)
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
            
            // Only allow if assignment is for the student's class group
            if (assignment.ClassGroupId != student.ClassGroupId)
            {
                return Forbid();
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
                    Assignment = assignment,
                    Language = assignment.Language ?? "csharp"
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
            AvailableClassGroups = await _context.ClassGroups.ToListAsync(),
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

        // Reload available modules and class groups for dropdown
        model.AvailableModules = await _context.SubjectModules.ToListAsync();
        model.AvailableClassGroups = await _context.ClassGroups.ToListAsync();

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
                CreatedAt = DateTime.UtcNow,
                Language = model.Language,
                ClassGroupId = model.ClassGroupId
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Add test cases - only add if both input and expected output are provided
            foreach (var testCaseModel in model.TestCases.Where(tc => 
                !string.IsNullOrEmpty(tc.Input) && !string.IsNullOrEmpty(tc.ExpectedOutput)))
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
            AvailableClassGroups = await _context.ClassGroups.ToListAsync(),
            Language = assignment.Language,
            ClassGroupId = assignment.ClassGroupId,
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

        // Reload available modules and class groups for dropdown
        model.AvailableModules = await _context.SubjectModules.ToListAsync();
        model.AvailableClassGroups = await _context.ClassGroups.ToListAsync();

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
            assignment.Language = model.Language;
            assignment.ClassGroupId = model.ClassGroupId;

            // Update test cases - only add if both input and expected output are provided
            _context.TestCases.RemoveRange(assignment.TestCases);
            
            foreach (var testCaseModel in model.TestCases.Where(tc => 
                !string.IsNullOrEmpty(tc.Input) && !string.IsNullOrEmpty(tc.ExpectedOutput)))
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
    // GET: Assignments/Submit/5
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students
            .Include(s => s.ClassGroup)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .Include(a => a.SubjectModule)
            .Include(a => a.TestCases)
            .Include(a => a.ClassGroup)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            return NotFound();
        }

        // Check if assignment is active and not past due date
        if (!assignment.IsActive || assignment.DueDate <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Задачата е неактивна или крайният срок е изтекъл.";
            return RedirectToAction("Details", new { id = id });
        }

        // Check if student can access this assignment
        if (assignment.ClassGroupId != student.ClassGroupId)
        {
            return Forbid();
        }

        var publicTestCases = assignment.TestCases.Where(tc => !tc.IsHidden).ToList();
        var studentSubmissions = await _context.Submissions
            .Where(s => s.StudentId == student.Id && s.AssignmentId == assignment.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        // Get grades for these submissions
        var grades = await _context.Grades
            .Where(g => g.StudentId == student.Id && g.AssignmentId == assignment.Id)
            .ToListAsync();

        // Create a dictionary to map assignment IDs to grades
        var gradeLookup = grades.ToDictionary(g => g.AssignmentId, g => g);

        var viewModel = new SubmissionViewModel
        {
            AssignmentId = assignment.Id,
            Assignment = assignment,
            AvailableLanguages = new List<string> { "csharp", "python", "java", "javascript" },
            Language = assignment.Language ?? "csharp"
        };

        ViewBag.PublicTestCases = publicTestCases;
        ViewBag.StudentSubmissions = studentSubmissions;
        ViewBag.GradeLookup = gradeLookup;
        ViewBag.CanSubmit = assignment.DueDate > DateTime.UtcNow;
        ViewBag.TimeRemaining = assignment.DueDate > DateTime.UtcNow 
            ? $"{(assignment.DueDate - DateTime.UtcNow).Days} дни" 
            : "Изтекъл";

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int id, SubmissionViewModel model)
    {
        _logger.LogInformation("Submission attempt - Route ID: {RouteId}, Model AssignmentId: {ModelId}", id, model.AssignmentId);
        
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            _logger.LogError("Student not found for user ID: {UserId}", userId);
            return NotFound();
        }

        // Use the route parameter if model.AssignmentId is 0 or null
        var assignmentId = model.AssignmentId > 0 ? model.AssignmentId : id;
        model.AssignmentId = assignmentId;
        
        _logger.LogInformation("Final assignment ID: {AssignmentId}", assignmentId);
        
        var assignment = await _context.Assignments
            .Include(a => a.TestCases)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound();
        }

        // Check if assignment is still active and not past due date
        if (!assignment.IsActive || assignment.DueDate <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Задачата е неактивна или крайният срок е изтекъл.";
            
            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = TempData["ErrorMessage"] });
            }
            
            return RedirectToAction("Submit", new { id = assignmentId });
        }

        if (ModelState.IsValid)
        {
            _logger.LogInformation("Creating submission for student {StudentId}, assignment {AssignmentId}", student.Id, assignment.Id);
            
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
            
            _logger.LogInformation("Submission created with ID: {SubmissionId}", submission.Id);

            // Execute code against test cases - handle empty test cases
            try
            {
                _logger.LogInformation("Starting code execution for submission {SubmissionId}", submission.Id);
                
                submission.Status = SubmissionStatus.Running;
                await _context.SaveChangesAsync();

                if (assignment.TestCases.Any())
                {
                    _logger.LogInformation("Executing {TestCaseCount} test cases for submission {SubmissionId}", 
                        assignment.TestCases.Count, submission.Id);
                    
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
                        result.SubmissionId = submission.Id;
                        _context.ExecutionResults.Add(result);
                    }
                    
                    _logger.LogInformation("Execution completed for submission {SubmissionId}. Results: {ResultCount}", 
                        submission.Id, executionResults.Count);
                }
                else
                {
                    _logger.LogInformation("No test cases found for assignment {AssignmentId}. Running compilation check only.", assignment.Id);
                    
                    // No test cases - just compile and run the code to check for errors
                    var dummyTestCase = new TestCase
                    {
                        Input = "",
                        ExpectedOutput = "",
                        Points = 0
                    };
                    
                    var result = await _codeExecutor.ExecuteCodeAsync(
                        submission.Code,
                        submission.Language,
                        dummyTestCase,
                        assignment.TimeLimit,
                        assignment.MemoryLimit
                    );
                    
                    // Store the result for compilation/runtime error checking
                    result.SubmissionId = submission.Id;
                    result.TestCaseId = null; // No test case for assignments without test cases
                    _context.ExecutionResults.Add(result);
                }
                
                await _context.SaveChangesAsync();

                // Use the grade calculation service for consistent grading
                _logger.LogInformation("Calculating grade for submission {SubmissionId}", submission.Id);
                
                await _gradeCalculationService.UpdateSubmissionScoreAsync(submission.Id);
                var grade = await _gradeCalculationService.CalculateGradeAsync(submission.Id);

                _logger.LogInformation("Grade calculated for submission {SubmissionId}: {Points} points (Grade: {GradeValue})", 
                    submission.Id, grade.Points, grade.GradeValue);
                
                submission.Status = SubmissionStatus.Completed;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Решението е подадено и оценено успешно! Резултат: {grade.Points} точки (Оценка: {grade.GradeValue}).";
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = TempData["SuccessMessage"] });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing submission {SubmissionId}", submission.Id);
                submission.Status = SubmissionStatus.RuntimeError;
                await _context.SaveChangesAsync();
                
                TempData["ErrorMessage"] = "Възникна грешка при изпълнението на кода. Моля, опитайте отново.";
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = TempData["ErrorMessage"] });
                }
            }
            
            return RedirectToAction("Submit", new { id = assignmentId });
        }

        // ModelState is invalid
        var errorMessages = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        
        TempData["ErrorMessage"] = "Грешка при подаването на решението: " + string.Join(", ", errorMessages);
        
        // Check if this is an AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = false, message = TempData["ErrorMessage"] });
        }
        
        return RedirectToAction("Submit", new { id = assignmentId });
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

            // Get public test cases only - handle empty test cases
            var publicTestCases = assignment.TestCases.Where(tc => !tc.IsHidden).ToList();
            if (!publicTestCases.Any())
            {
                return Json(new { success = false, error = "Няма налични тестови случаи за тази задача" });
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
                    actualOutput = result.ActualOutput,
                    success = result.IsCorrect,
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

    // GET: Assignments/GetSubmissionStatus/5
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetSubmissionStatus(int id)
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
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == student.Id);

            if (submission == null)
            {
                return Json(new { success = false, error = "Submission not found" });
            }

            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == student.Id && g.AssignmentId == submission.AssignmentId);

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
                    actualOutput = er.ActualOutput,
                    isSuccess = er.IsCorrect,
                    executionTime = er.ExecutionTime,
                    status = er.Status.ToString(),
                    errorMessage = er.ErrorMessage,
                    points = er.IsCorrect ? er.TestCase.Points : 0
                }).ToList(),
                grade = grade != null ? new
                {
                    points = grade.Points,
                    gradeValue = grade.GradeValue,
                    gradedAt = grade.GradedAt.ToString("dd.MM.yyyy HH:mm")
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