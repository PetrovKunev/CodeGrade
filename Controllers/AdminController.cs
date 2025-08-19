using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CodeGrade.Data;
using CodeGrade.Models;
using CodeGrade.ViewModels;

namespace CodeGrade.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context, 
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var teacherCodes = await _context.TeacherRegistrationCodes
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(teacherCodes);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateTeacherCode(string description, int? expiresInDays)
        {
            try
            {
                var code = GenerateUniqueCode();
                var expiresAt = expiresInDays.HasValue ? DateTime.UtcNow.AddDays(expiresInDays.Value) : (DateTime?)null;

                var teacherCode = new TeacherRegistrationCode
                {
                    Code = code,
                    Description = description ?? "Код за регистрация на преподавател",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _context.TeacherRegistrationCodes.Add(teacherCode);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated teacher registration code: {Code} by admin {AdminId}", 
                    code, User.Identity?.Name);

                TempData["SuccessMessage"] = $"Кодът '{code}' е генериран успешно!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating teacher registration code");
                TempData["ErrorMessage"] = "Грешка при генериране на кода.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeacherCode(int id)
        {
            try
            {
                var code = await _context.TeacherRegistrationCodes.FindAsync(id);
                if (code != null)
                {
                    _context.TeacherRegistrationCodes.Remove(code);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted teacher registration code: {Code} by admin {AdminId}", 
                        code.Code, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Кодът е изтрит успешно!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting teacher registration code {Id}", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на кода.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            
            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.TeacherRegistrationCodes.Any(c => c.Code == code));

            return code;
        }

        #region Users Management
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userViewModels = new List<AdminUserViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList(),
                    StudentId = user.Student?.Id.ToString(),
                    TeacherId = user.Teacher?.Id.ToString()
                });
            }

            return View(userViewModels);
        }

        public IActionResult CreateUser()
        {
            var viewModel = new AdminUserCreateViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(AdminUserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                        
                        // Create Student or Teacher record based on role
                        if (model.Role == "Student")
                        {
                            var student = new Student { UserId = user.Id };
                            _context.Students.Add(student);
                        }
                        else if (model.Role == "Teacher")
                        {
                            var teacher = new Teacher { UserId = user.Id };
                            _context.Teachers.Add(teacher);
                        }

                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created user {UserId} with role {Role} by admin {AdminId}", 
                            user.Id, model.Role, User.Identity?.Name);

                        TempData["SuccessMessage"] = "Потребителят е създаден успешно!";
                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Грешка при създаване на потребителя.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var viewModel = new AdminUserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(model.Id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email;
                    user.UserName = model.UserName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.IsActive = model.IsActive;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        // Update roles
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRolesAsync(user, model.Roles);

                        _logger.LogInformation("Updated user {UserId} by admin {AdminId}", 
                            user.Id, User.Identity?.Name);

                        TempData["SuccessMessage"] = "Потребителят е обновен успешно!";
                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId}", model.Id);
                    ModelState.AddModelError("", "Грешка при обновяване на потребителя.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    // Проверяваме зависимостите преди изтриване
                    var dependencies = await CheckUserDependenciesAsync(id);
                    
                    if (dependencies.HasDependencies)
                    {
                        // Ако има зависимости, показваме информация за тях
                        TempData["WarningMessage"] = $"Потребителят не може да бъде изтрит, тъй като има прикачени данни. {dependencies.GetDependenciesSummary()}";
                        return RedirectToAction(nameof(Users));
                    }

                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Deleted user {UserId} by admin {AdminId}", 
                            user.Id, User.Identity?.Name);

                        TempData["SuccessMessage"] = "Потребителят е изтрит успешно!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Грешка при изтриване на потребителя.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на потребителя.";
            }

            return RedirectToAction(nameof(Users));
        }

        private async Task<UserDependenciesInfo> CheckUserDependenciesAsync(string userId)
        {
            var dependencies = new UserDependenciesInfo();

            // Проверяваме дали потребителят е студент
            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.Submissions)
                    .ThenInclude(sub => sub.ExecutionResults)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student != null)
            {
                dependencies.IsStudent = true;
                dependencies.GradesCount = student.Grades.Count;
                dependencies.SubmissionsCount = student.Submissions.Count;
                dependencies.ExecutionResultsCount = student.Submissions.Sum(s => s.ExecutionResults.Count);
            }

            // Проверяваме дали потребителят е учител
            var teacher = await _context.Teachers
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher != null)
            {
                dependencies.IsTeacher = true;
                dependencies.AssignmentsCount = teacher.Assignments.Count;
            }

            return dependencies;
        }



        public async Task<IActionResult> UserDependencies(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var dependencies = await CheckUserDependenciesAsync(id);
            var viewModel = new UserDependenciesViewModel
            {
                UserId = id,
                UserName = $"{user.FirstName} {user.LastName}",
                UserEmail = user.Email ?? string.Empty,
                Dependencies = dependencies
            };

            if (dependencies.IsStudent)
            {
                var student = await _context.Students
                    .Include(s => s.Grades)
                        .ThenInclude(g => g.Assignment)
                    .Include(s => s.Submissions)
                        .ThenInclude(sub => sub.Assignment)
                    .Include(s => s.Submissions)
                        .ThenInclude(sub => sub.ExecutionResults)
                    .FirstOrDefaultAsync(s => s.UserId == id);

                if (student != null)
                {
                    viewModel.Grades = student.Grades.Select(g => new GradeInfo
                    {
                        Id = g.Id,
                        AssignmentName = g.Assignment.Title,
                        Points = g.Points,
                        GradeValue = g.GradeValue,
                        GradedAt = g.GradedAt
                    }).ToList();

                    viewModel.Submissions = student.Submissions.Select(s => new SubmissionInfo
                    {
                        Id = s.Id,
                        AssignmentName = s.Assignment.Title,
                        Status = s.Status.ToString(),
                        Score = s.Score,
                        SubmittedAt = s.SubmittedAt,
                        ExecutionResultsCount = s.ExecutionResults.Count
                    }).ToList();
                }
            }

            if (dependencies.IsTeacher)
            {
                var teacher = await _context.Teachers
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.SubjectModule)
                    .FirstOrDefaultAsync(t => t.UserId == id);

                if (teacher != null)
                {
                    viewModel.Assignments = teacher.Assignments.Select(a => new AssignmentInfo
                    {
                        Id = a.Id,
                        Title = a.Title,
                        SubjectModuleName = a.SubjectModule.Name,
                        CreatedAt = a.CreatedAt,
                        DueDate = a.DueDate
                    }).ToList();
                }
            }

            return View(viewModel);
        }

        // Actions за изтриване на зависимости
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserGrades(string userId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Grades)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student != null && student.Grades.Any())
                {
                    _context.Grades.RemoveRange(student.Grades);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted {GradeCount} grades for user {UserId} by admin {AdminId}", 
                        student.Grades.Count, userId, User.Identity?.Name);

                    TempData["SuccessMessage"] = $"Успешно изтрити {student.Grades.Count} оценки.";
                }
                else
                {
                    TempData["InfoMessage"] = "Няма оценки за изтриване.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting grades for user {UserId}", userId);
                TempData["ErrorMessage"] = "Грешка при изтриване на оценките.";
            }

            return RedirectToAction(nameof(UserDependencies), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserSubmissions(string userId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Submissions)
                        .ThenInclude(sub => sub.ExecutionResults)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student != null && student.Submissions.Any())
                {
                    // Първо изтриваме ExecutionResults
                    var executionResults = student.Submissions.SelectMany(s => s.ExecutionResults).ToList();
                    _context.ExecutionResults.RemoveRange(executionResults);

                    // След това изтриваме Submissions
                    _context.Submissions.RemoveRange(student.Submissions);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted {SubmissionCount} submissions and {ExecutionResultCount} execution results for user {UserId} by admin {AdminId}", 
                        student.Submissions.Count, executionResults.Count, userId, User.Identity?.Name);

                    TempData["SuccessMessage"] = $"Успешно изтрити {student.Submissions.Count} решения и {executionResults.Count} резултати от изпълнение.";
                }
                else
                {
                    TempData["InfoMessage"] = "Няма решения за изтриване.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting submissions for user {UserId}", userId);
                TempData["ErrorMessage"] = "Грешка при изтриване на решенията.";
            }

            return RedirectToAction(nameof(UserDependencies), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserAssignments(string userId)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.TestCases)
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.Submissions)
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher != null && teacher.Assignments.Any())
                {
                    var assignments = teacher.Assignments.ToList();
                    var testCases = assignments.SelectMany(a => a.TestCases).ToList();
                    var submissions = assignments.SelectMany(a => a.Submissions).ToList();

                    // Първо изтриваме свързаните данни
                    if (submissions.Any())
                    {
                        var executionResults = await _context.ExecutionResults
                            .Where(er => submissions.Select(s => s.Id).Contains(er.SubmissionId))
                            .ToListAsync();
                        _context.ExecutionResults.RemoveRange(executionResults);
                    }

                    _context.Submissions.RemoveRange(submissions);
                    _context.TestCases.RemoveRange(testCases);
                    _context.Assignments.RemoveRange(assignments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted {AssignmentCount} assignments, {TestCaseCount} test cases, and {SubmissionCount} submissions for teacher {UserId} by admin {AdminId}", 
                        assignments.Count, testCases.Count, submissions.Count, userId, User.Identity?.Name);

                    TempData["SuccessMessage"] = $"Успешно изтрити {assignments.Count} задачи, {testCases.Count} тестови случаи и {submissions.Count} решения.";
                }
                else
                {
                    TempData["InfoMessage"] = "Няма задачи за изтриване.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignments for user {UserId}", userId);
                TempData["ErrorMessage"] = "Грешка при изтриване на задачите.";
            }

            return RedirectToAction(nameof(UserDependencies), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllUserDependencies(string userId)
        {
            try
            {
                // Изтриваме всички зависимости наведнъж
                await DeleteUserGrades(userId);
                await DeleteUserSubmissions(userId);
                await DeleteUserAssignments(userId);

                // Проверяваме дали вече няма зависимости
                var dependencies = await CheckUserDependenciesAsync(userId);
                if (!dependencies.HasDependencies)
                {
                    TempData["SuccessMessage"] = "Всички зависимости са изтрити успешно. Сега можете да изтриете потребителя.";
                }
                else
                {
                    TempData["WarningMessage"] = "Някои зависимости все още съществуват.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all dependencies for user {UserId}", userId);
                TempData["ErrorMessage"] = "Грешка при изтриване на всички зависимости.";
            }

            return RedirectToAction(nameof(UserDependencies), new { id = userId });
        }
        #endregion

        #region Class Groups Management
        public async Task<IActionResult> ClassGroups()
        {
            var classGroups = await _context.ClassGroups
                .Include(cg => cg.Students)
                .OrderBy(cg => cg.Year)
                .ThenBy(cg => cg.Name)
                .ToListAsync();

            var viewModels = classGroups.Select(cg => new AdminClassGroupViewModel
            {
                Id = cg.Id,
                Name = cg.Name,
                Description = cg.Description,
                Year = cg.Year,
                StudentCount = cg.Students.Count,
                CreatedAt = DateTime.UtcNow, // You might want to add this field to your model
                Students = cg.Students.Select(s => new StudentViewModel
                {
                    Id = s.Id.ToString(),
                    FirstName = s.User?.FirstName ?? "",
                    LastName = s.User?.LastName ?? "",
                    Email = s.User?.Email ?? ""
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        public IActionResult CreateClassGroup()
        {
            var viewModel = new AdminClassGroupCreateViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClassGroup(AdminClassGroupCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var classGroup = new ClassGroup
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Year = model.Year
                    };

                    _context.ClassGroups.Add(classGroup);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created class group {ClassGroupId} by admin {AdminId}", 
                        classGroup.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Класът е създаден успешно!";
                    return RedirectToAction(nameof(ClassGroups));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating class group");
                    ModelState.AddModelError("", "Грешка при създаване на класа.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> EditClassGroup(int id)
        {
            var classGroup = await _context.ClassGroups.FindAsync(id);
            if (classGroup == null)
            {
                return NotFound();
            }

            var viewModel = new AdminClassGroupEditViewModel
            {
                Id = classGroup.Id,
                Name = classGroup.Name,
                Description = classGroup.Description,
                Year = classGroup.Year
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClassGroup(AdminClassGroupEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var classGroup = await _context.ClassGroups.FindAsync(model.Id);
                    if (classGroup == null)
                    {
                        return NotFound();
                    }

                    classGroup.Name = model.Name;
                    classGroup.Description = model.Description;
                    classGroup.Year = model.Year;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated class group {ClassGroupId} by admin {AdminId}", 
                        classGroup.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Класът е обновен успешно!";
                    return RedirectToAction(nameof(ClassGroups));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating class group {ClassGroupId}", model.Id);
                    ModelState.AddModelError("", "Грешка при обновяване на класа.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassGroup(int id)
        {
            try
            {
                var classGroup = await _context.ClassGroups.FindAsync(id);
                if (classGroup != null)
                {
                    _context.ClassGroups.Remove(classGroup);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted class group {ClassGroupId} by admin {AdminId}", 
                        classGroup.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Класът е изтрит успешно!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting class group {ClassGroupId}", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на класа.";
            }

            return RedirectToAction(nameof(ClassGroups));
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentsForClassGroup(int id)
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassGroupId == id)
                .Select(s => new { 
                    fullName = s.User.FirstName + " " + s.User.LastName, 
                    email = s.User.Email 
                })
                .ToListAsync();

            return Json(students);
        }
        #endregion

        #region Modules Management
        public async Task<IActionResult> Modules()
        {
            var modules = await _context.SubjectModules
                .Include(sm => sm.Assignments)
                .OrderBy(sm => sm.Semester)
                .ThenBy(sm => sm.Name)
                .ToListAsync();

            var viewModels = modules.Select(sm => new AdminModuleViewModel
            {
                Id = sm.Id,
                Name = sm.Name,
                Description = sm.Description,
                Semester = sm.Semester,
                AssignmentCount = sm.Assignments.Count,
                CreatedAt = DateTime.UtcNow, // You might want to add this field to your model
                Assignments = sm.Assignments.Select(a => new AssignmentViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    DueDate = a.DueDate,
                    MaxPoints = a.MaxPoints,
                    IsActive = a.IsActive
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        public IActionResult CreateModule()
        {
            var viewModel = new AdminModuleCreateViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(AdminModuleCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var module = new SubjectModule
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Semester = model.Semester
                    };

                    _context.SubjectModules.Add(module);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created module {ModuleId} by admin {AdminId}", 
                        module.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Модулът е създаден успешно!";
                    return RedirectToAction(nameof(Modules));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating module");
                    ModelState.AddModelError("", "Грешка при създаване на модула.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> EditModule(int id)
        {
            var module = await _context.SubjectModules.FindAsync(id);
            if (module == null)
            {
                return NotFound();
            }

            var viewModel = new AdminModuleEditViewModel
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                Semester = module.Semester
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModule(AdminModuleEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var module = await _context.SubjectModules.FindAsync(model.Id);
                    if (module == null)
                    {
                        return NotFound();
                    }

                    module.Name = model.Name;
                    module.Description = model.Description;
                    module.Semester = model.Semester;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated module {ModuleId} by admin {AdminId}", 
                        module.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Модулът е обновен успешно!";
                    return RedirectToAction(nameof(Modules));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating module {ModuleId}", model.Id);
                    ModelState.AddModelError("", "Грешка при обновяване на модула.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                var module = await _context.SubjectModules.FindAsync(id);
                if (module != null)
                {
                    _context.SubjectModules.Remove(module);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted module {ModuleId} by admin {AdminId}", 
                        module.Id, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Модулът е изтрит успешно!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting module {ModuleId}", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на модула.";
            }

            return RedirectToAction(nameof(Modules));
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignmentsForModule(int id)
        {
            var assignments = await _context.Assignments
                .Where(a => a.SubjectModuleId == id)
                .Select(a => new { 
                    title = a.Title, 
                    dueDate = a.DueDate, 
                    maxPoints = a.MaxPoints, 
                    isActive = a.IsActive 
                })
                .ToListAsync();

            return Json(assignments);
        }

        // Actions за проверка на зависимостите на класове и модули
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassGroupWithDependencies(int id)
        {
            try
            {
                var classGroup = await _context.ClassGroups
                    .Include(cg => cg.Students)
                    .FirstOrDefaultAsync(cg => cg.Id == id);

                if (classGroup == null)
                {
                    return NotFound();
                }

                if (classGroup.Students.Any())
                {
                    // Премахваме връзката между студентите и класа
                    foreach (var student in classGroup.Students)
                    {
                        student.ClassGroupId = null;
                    }
                    await _context.SaveChangesAsync();
                }

                _context.ClassGroups.Remove(classGroup);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted class group {ClassGroupId} with dependencies by admin {AdminId}", 
                    id, User.Identity?.Name);

                TempData["SuccessMessage"] = "Класът е изтрит успешно заедно с всички зависимости!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting class group {ClassGroupId} with dependencies", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на класа.";
            }

            return RedirectToAction(nameof(ClassGroups));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModuleWithDependencies(int id)
        {
            try
            {
                var module = await _context.SubjectModules
                    .Include(sm => sm.Assignments)
                        .ThenInclude(a => a.TestCases)
                    .Include(sm => sm.Assignments)
                        .ThenInclude(a => a.Submissions)
                    .FirstOrDefaultAsync(sm => sm.Id == id);

                if (module == null)
                {
                    return NotFound();
                }

                var assignments = module.Assignments.ToList();
                var testCases = assignments.SelectMany(a => a.TestCases).ToList();
                var submissions = assignments.SelectMany(a => a.Submissions).ToList();

                // Първо изтриваме свързаните данни
                if (submissions.Any())
                {
                    var executionResults = await _context.ExecutionResults
                        .Where(er => submissions.Select(s => s.Id).Contains(er.SubmissionId))
                        .ToListAsync();
                    _context.ExecutionResults.RemoveRange(executionResults);
                }

                _context.Submissions.RemoveRange(submissions);
                _context.TestCases.RemoveRange(testCases);
                _context.Assignments.RemoveRange(assignments);
                _context.SubjectModules.Remove(module);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted module {ModuleId} with all dependencies by admin {AdminId}", 
                    id, User.Identity?.Name);

                TempData["SuccessMessage"] = $"Модулът е изтрит успешно заедно с {assignments.Count} задачи, {testCases.Count} тестови случая и {submissions.Count} решения!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting module {ModuleId} with dependencies", id);
                TempData["ErrorMessage"] = "Грешка при изтриване на модула.";
            }

            return RedirectToAction(nameof(Modules));
        }
        #endregion
    }
} 