using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;

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
} 