using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;
using CodeGrade.Data;

namespace CodeGrade.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
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
} 