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

            // Ensure database is created and apply all migrations
            await context.Database.MigrateAsync();

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
                    new SubjectModule { Name = "Mодул 1 - Обектно ориентирано проектиране и програмиране", Description = "Обектно ориентирано проектиране и програмиране", Semester = 1 },
                    new SubjectModule { Name = "Mодул 2 - Структури от данни и алгоритми", Description = "Структури от данни и алгоритми", Semester = 2 },
                    new SubjectModule { Name = "Модул 3 - Релационен модел на бази от данни", Description = "Релационен модел на бази от данни", Semester = 3 },
                    new SubjectModule { Name = "Модул 4 - Програмиране на информационни системи", Description = "Програмиране на информационни системи", Semester = 4 }
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
                    FirstName = "Явор",
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
                        Title = "Преподавател"
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

                    var classGroup11A = context.ClassGroups.FirstOrDefault(cg => cg.Name == "11А");
                    var student = new Student
                    {
                        UserId = studentUser.Id,
                        StudentNumber = "2024001",
                        ClassGroupId = classGroup11A?.Id,
                        SubGroup = "1",
                        ClassNumber = 15
                    };

                    context.Students.Add(student);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
} 