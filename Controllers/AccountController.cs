using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CodeGrade.ViewModels;
using CodeGrade.Models;
using CodeGrade.Data;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Services;

namespace CodeGrade.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
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
                    IsActive = true,
                    EmailConfirmed = false // ✅ Нова регистрация не е потвърдена
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
                    
                    // Генериране на email потвърждение токен
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", 
                        new { userId = user.Id, token = token }, Request.Scheme, Request.Host.Value);
                    
                    try
                    {
                        // Изпращане на email за потвърждение
                        if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(confirmationLink))
                        {
                            await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);
                        }
                        
                        TempData["InfoMessage"] = "📧 Регистрацията е успешна! Моля, проверете вашия имейл за потвърждение на акаунта.";
                        return RedirectToAction("Login");
                    }
                    catch (Exception ex)
                    {
                        // Ако email не може да се изпрати, все пак създаваме потребителя
                        // но го маркираме като неактивен докато потвърди имейла
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                        
                        TempData["WarningMessage"] = "⚠️ Регистрацията е успешна, но не можахме да изпратим email за потвърждение. Моля, свържете се с администратор.";
                        return RedirectToAction("Login");
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

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "❌ Потребителят не е намерен.";
                return RedirectToAction("Login");
            }
            
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Изпращане на поздравителен имейл
                try
                {
                    if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.FirstName))
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }
                
                TempData["SuccessMessage"] = "✅ Имейлът е потвърден успешно! Сега можете да влезете в системата.";
                return RedirectToAction("Login");
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Грешка при потвърждаване на имейла.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Не показваме грешка за да не разкриваме съществуващи имейли
                TempData["InfoMessage"] = "📧 Ако имейлът съществува, ще получите линк за потвърждение.";
                return RedirectToAction("Login");
            }
            
            if (user.EmailConfirmed)
            {
                TempData["InfoMessage"] = "ℹ️ Вашият имейл вече е потвърден.";
                return RedirectToAction("Login");
            }
            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", 
                new { userId = user.Id, token = token }, Request.Scheme, Request.Host.Value);
            
            try
            {
                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(confirmationLink))
                {
                    await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);
                }
                TempData["SuccessMessage"] = "📧 Email за потвърждение е изпратен отново.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to {Email}", user.Email);
                TempData["ErrorMessage"] = "❌ Не можахме да изпратим email за потвърждение. Моля, опитайте отново по-късно.";
            }
            
            return RedirectToAction("Login");
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