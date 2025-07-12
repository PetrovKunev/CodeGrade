using CodeGrade.Services;
using CodeGrade.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeGrade.Tests
{
    /// <summary>
    /// Примерен тест за Judge0 API функционалност
    /// Този файл може да се използва за тестване на Judge0 интеграцията
    /// </summary>
    public class TestJudge0API
    {
        public static async Task TestJudge0Integration()
        {
            // Конфигурация
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<Judge0CodeExecutorService>();

            var judge0Service = new Judge0CodeExecutorService(configuration, logger);

            // Тестови случаи за различни езици
            var testCases = new[]
            {
                new { Language = "csharp", Code = "Console.WriteLine(\"Hello World\");", Expected = "Hello World" },
                new { Language = "python", Code = "print(\"Hello World\")", Expected = "Hello World" },
                new { Language = "java", Code = "public class Main { public static void main(String[] args) { System.out.println(\"Hello World\"); } }", Expected = "Hello World" },
                new { Language = "javascript", Code = "console.log(\"Hello World\")", Expected = "Hello World" }
            };

            foreach (var test in testCases)
            {
                Console.WriteLine($"\n=== Тестване на {test.Language.ToUpper()} ===");
                
                try
                {
                    var testCase = new TestCase
                    {
                        Input = "",
                        ExpectedOutput = test.Expected,
                        Points = 10
                    };

                    var result = await judge0Service.ExecuteCodeAsync(
                        test.Code,
                        test.Language,
                        testCase,
                        30, // 30 секунди timeout
                        128  // 128MB памет
                    );

                    Console.WriteLine($"Статус: {result.Status}");
                    Console.WriteLine($"Резултат: {(result.IsCorrect ? "✅ Правилно" : "❌ Грешно")}");
                    Console.WriteLine($"Изход: {result.ActualOutput}");
                    Console.WriteLine($"Време: {result.ExecutionTime}ms");
                    Console.WriteLine($"Памет: {result.MemoryUsed}KB");
                    
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        Console.WriteLine($"Грешка: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Грешка при тестване на {test.Language}: {ex.Message}");
                }
            }
        }

        public static async Task TestComplexCode()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<Judge0CodeExecutorService>();

            var judge0Service = new Judge0CodeExecutorService(configuration, logger);

            // Сложен тест за C# - сума на числа
            var csharpCode = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        string input = Console.ReadLine();
        var numbers = input.Split(' ').Select(int.Parse).ToArray();
        int sum = numbers.Sum();
        Console.WriteLine(sum);
    }
}";

            var testCase = new TestCase
            {
                Input = "1 2 3 4 5",
                ExpectedOutput = "15",
                Points = 20
            };

            Console.WriteLine("\n=== Сложен тест за C# ===");
            Console.WriteLine("Вход: 1 2 3 4 5");
            Console.WriteLine("Очакван изход: 15");

            try
            {
                var result = await judge0Service.ExecuteCodeAsync(
                    csharpCode,
                    "csharp",
                    testCase,
                    30,
                    128
                );

                Console.WriteLine($"Статус: {result.Status}");
                Console.WriteLine($"Резултат: {(result.IsCorrect ? "✅ Правилно" : "❌ Грешно")}");
                Console.WriteLine($"Изход: {result.ActualOutput}");
                Console.WriteLine($"Време: {result.ExecutionTime}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Грешка: {ex.Message}");
            }
        }

        public static async Task TestErrorHandling()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<Judge0CodeExecutorService>();

            var judge0Service = new Judge0CodeExecutorService(configuration, logger);

            // Тест с грешен код
            var errorCode = @"
using System;

class Program
{
    static void Main()
    {
        int x = 10;
        int y = 0;
        int result = x / y; // Деление на нула
        Console.WriteLine(result);
    }
}";

            var testCase = new TestCase
            {
                Input = "",
                ExpectedOutput = "10",
                Points = 10
            };

            Console.WriteLine("\n=== Тест за обработка на грешки ===");

            try
            {
                var result = await judge0Service.ExecuteCodeAsync(
                    errorCode,
                    "csharp",
                    testCase,
                    30,
                    128
                );

                Console.WriteLine($"Статус: {result.Status}");
                Console.WriteLine($"Резултат: {(result.IsCorrect ? "✅ Правилно" : "❌ Грешно")}");
                Console.WriteLine($"Грешка: {result.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Системна грешка: {ex.Message}");
            }
        }
    }
} 