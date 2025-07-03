using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;
using CodeGrade.Services;

namespace CodeGrade.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly GradeCalculationService _gradeService;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        GradeCalculationService gradeService)
    {
        _logger = logger;
        _context = context;
        _gradeService = gradeService;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Student"))
            {
                return RedirectToAction("StudentDashboard");
            }
            else if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("TeacherDashboard");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
        }
        
        return View();
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentDashboard()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.ClassGroup)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return NotFound();
        }

        var activeAssignments = await _context.Assignments
            .Include(a => a.SubjectModule)
            .Where(a => a.IsActive && a.DueDate > DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .Take(5)
            .ToListAsync();

        var recentSubmissions = await _context.Submissions
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToListAsync();

        var statistics = await _gradeService.GetStudentStatisticsAsync(student.Id);

        ViewBag.Student = student;
        ViewBag.ActiveAssignments = activeAssignments;
        ViewBag.RecentSubmissions = recentSubmissions;
        ViewBag.Statistics = statistics;

        return View();
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> TeacherDashboard()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return NotFound();
        }

        var assignments = await _context.Assignments
            .Include(a => a.SubjectModule)
            .Where(a => a.TeacherId == teacher.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentSubmissions = await _context.Submissions
            .Include(s => s.Student)
            .Include(s => s.Assignment)
            .Where(s => s.Assignment.TeacherId == teacher.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Teacher = teacher;
        ViewBag.Assignments = assignments;
        ViewBag.RecentSubmissions = recentSubmissions;

        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var totalStudents = await _context.Students.CountAsync();
        var totalTeachers = await _context.Teachers.CountAsync();
        var totalAssignments = await _context.Assignments.CountAsync();
        var totalSubmissions = await _context.Submissions.CountAsync();

        ViewBag.TotalStudents = totalStudents;
        ViewBag.TotalTeachers = totalTeachers;
        ViewBag.TotalAssignments = totalAssignments;
        ViewBag.TotalSubmissions = totalSubmissions;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
