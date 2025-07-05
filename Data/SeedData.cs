using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;

namespace CodeGrade.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            string[] roles = { "Admin", "Teacher", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed admin user
            var adminUser = await userManager.FindByEmailAsync("admin@codegrade.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@codegrade.com",
                    Email = "admin@codegrade.com",
                    FirstName = "Администратор",
                    LastName = "Система",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed class groups
            if (!context.ClassGroups.Any())
            {
                var classGroups = new[]
                {
                    new ClassGroup { Name = "10А", Description = "10 клас А профил", Year = 2024 },
                    new ClassGroup { Name = "10Б", Description = "10 клас Б профил", Year = 2024 },
                    new ClassGroup { Name = "11А", Description = "11 клас А профил", Year = 2024 },
                    new ClassGroup { Name = "11Б", Description = "11 клас Б профил", Year = 2024 }
                };

                context.ClassGroups.AddRange(classGroups);
                await context.SaveChangesAsync();
            }

            // Seed subject modules
            if (!context.SubjectModules.Any())
            {
                var modules = new[]
                {
                    new SubjectModule { Name = "Въведение в програмирането", Description = "Основи на програмирането", Semester = 1 },
                    new SubjectModule { Name = "Обектно-ориентирано програмиране", Description = "ООП принципи", Semester = 2 },
                    new SubjectModule { Name = "Структури от данни", Description = "Масиви, списъци, стекове", Semester = 3 },
                    new SubjectModule { Name = "Алгоритми", Description = "Сортиране, търсене, графи", Semester = 4 }
                };

                context.SubjectModules.AddRange(modules);
                await context.SaveChangesAsync();
            }

            // Seed sample teacher
            var teacherUser = await userManager.FindByEmailAsync("teacher@codegrade.com");
            if (teacherUser == null)
            {
                teacherUser = new ApplicationUser
                {
                    UserName = "teacher@codegrade.com",
                    Email = "teacher@codegrade.com",
                    FirstName = "Иван",
                    LastName = "Петров",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(teacherUser, "Teacher123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacherUser, "Teacher");

                    var teacher = new Teacher
                    {
                        UserId = teacherUser.Id,
                        Department = "Информатика",
                        Title = "Ст. преподавател"
                    };

                    context.Teachers.Add(teacher);
                    await context.SaveChangesAsync();
                }
            }

            // Seed sample student
            var studentUser = await userManager.FindByEmailAsync("student@codegrade.com");
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = "student@codegrade.com",
                    Email = "student@codegrade.com",
                    FirstName = "Мария",
                    LastName = "Георгиева",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(studentUser, "Student123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");

                    var firstClassGroup = context.ClassGroups.First();
                    var student = new Student
                    {
                        UserId = studentUser.Id,
                        StudentNumber = "2024001",
                        ClassGroupId = firstClassGroup.Id
                    };

                    context.Students.Add(student);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
} 