using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CodeGrade.ViewModels;
using CodeGrade.Models;
using CodeGrade.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeGrade.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        // Redirect based on user role
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("AdminDashboard", "Home");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                        {
                            return RedirectToAction("TeacherDashboard", "Home");
                        }
                        else
                        {
                            return RedirectToAction("StudentDashboard", "Home");
                        }
                    }
                    
                    return RedirectToLocal(returnUrl);
                }
                
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Акаунтът е временно заключен. Опитайте отново по-късно.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Невалиден имейл или парола.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Зареждаме класовете за dropdown
            var classGroups = _context.ClassGroups.ToList();
            ViewBag.ClassGroups = classGroups;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Зареждаме класовете за dropdown при неуспешна регистрация
            var classGroups = _context.ClassGroups.ToList();
            ViewBag.ClassGroups = classGroups;

            // Debug: Log model data
            System.Diagnostics.Debug.WriteLine($"Role: {model.Role}");
            System.Diagnostics.Debug.WriteLine($"ClassNumber: {model.ClassNumber}");
            System.Diagnostics.Debug.WriteLine($"ClassGroupId: {model.ClassGroupId}");

            // Debug: Log validation errors
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            // Check for duplicate ClassNumber within the same ClassGroup for students
            if (model.Role == "Student" && model.ClassGroupId.HasValue && model.ClassNumber.HasValue)
            {
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.ClassGroupId == model.ClassGroupId && s.ClassNumber == model.ClassNumber);
                
                if (existingStudent != null)
                {
                    ModelState.AddModelError(nameof(model.ClassNumber), 
                        $"Ученик с номер {model.ClassNumber} вече съществува в този клас.");
                    return View(model);
                }
            }

            if (ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState is valid, proceeding with user creation");
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                System.Diagnostics.Debug.WriteLine($"User creation result: {result.Succeeded}");
                if (result.Succeeded)
                {
                    // Add user to selected role
                    await _userManager.AddToRoleAsync(user, model.Role);
                    
                    if (model.Role == "Student")
                    {
                        // Create Student record
                        var student = new Student
                        {
                            UserId = user.Id,
                            StudentNumber = model.ClassNumber.HasValue ? model.ClassNumber.Value.ToString() : string.Empty,
                            ClassGroupId = model.ClassGroupId,
                            SubGroup = model.SubGroup,
                            ClassNumber = model.ClassNumber ?? 0
                        };
                        _context.Students.Add(student);
                    }
                    else if (model.Role == "Teacher")
                    {
                        // Create Teacher record
                        var teacher = new Teacher
                        {
                            UserId = user.Id,
                            Department = "Информатика",
                            Title = "Преподавател"
                        };
                        
                        _context.Teachers.Add(teacher);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    TempData["SuccessMessage"] = "🎉 Регистрацията е успешна! Добре дошли в CodeGrade.";
                    
                    // Redirect based on role
                    if (model.Role == "Teacher")
                    {
                        return RedirectToAction("TeacherDashboard", "Home");
                    }
                    else
                    {
                        return RedirectToAction("StudentDashboard", "Home");
                    }
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
} 