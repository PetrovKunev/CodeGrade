using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;

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

    // GET: Submissions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var submission = await _context.Submissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Assignment)
                .ThenInclude(a => a.SubjectModule)
            .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        // Check if user can view this submission
        if (User.IsInRole("Teacher"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            
            if (teacher == null || submission.Assignment.TeacherId != teacher.Id)
            {
                return Forbid();
            }
            
            return View("TeacherSubmissionDetails", submission);
        }
        else if (User.IsInRole("Student"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null || submission.StudentId != student.Id)
            {
                return Forbid();
            }
            
            return View("StudentSubmissionDetails", submission);
        }

        return Forbid();
    }
} 