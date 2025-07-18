using CodeGrade.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace CodeGrade.Services
{
    public class Judge0CodeExecutorService : ICodeExecutorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Judge0CodeExecutorService> _logger;

        public Judge0CodeExecutorService(IConfiguration configuration, ILogger<Judge0CodeExecutorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = new HttpClient();
            
            // Настройка на API ключа - използваме RapidAPI Judge0 CE
            var apiKey = _configuration["Judge0:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Judge0 API key is not configured. Please add 'Judge0:ApiKey' to appsettings.json");
                throw new InvalidOperationException("Judge0 API key is not configured");
            }
            
            // RapidAPI Judge0 CE конфигурация
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "judge0-ce.p.rapidapi.com");
            
            _logger.LogInformation("Judge0CodeExecutorService initialized with API key: {ApiKeyPrefix}...", apiKey.Substring(0, Math.Min(8, apiKey.Length)));
        }

        public async Task<ExecutionResult> ExecuteCodeAsync(
            string code, 
            string language, 
            TestCase testCase,
            int timeLimit,
            int memoryLimit,
            Action<ExecutionStatus>? statusCallback = null)
        {
            var startTime = DateTime.UtcNow;
            statusCallback?.Invoke(ExecutionStatus.Queued);

            try
            {
                // Validate and clean test case data
                var cleanInput = string.IsNullOrWhiteSpace(testCase.Input) ? "" : testCase.Input.Trim();
                var cleanExpectedOutput = string.IsNullOrWhiteSpace(testCase.ExpectedOutput) ? "" : testCase.ExpectedOutput.Trim();
                
                // 1. Създаване на submission според Judge0 CE документацията
                var submission = new Judge0Submission
                {
                    SourceCode = code,
                    LanguageId = GetLanguageId(language),
                    Stdin = cleanInput,
                    CpuTimeLimit = timeLimit,
                    MemoryLimit = memoryLimit * 1024 // Convert MB to KB
                };

                // Log the submission details for debugging
                _logger.LogInformation("Creating Judge0 submission - Language: {Language}, LanguageId: {LanguageId}, TimeLimit: {TimeLimit}, MemoryLimit: {MemoryLimit}", 
                    language, submission.LanguageId, submission.CpuTimeLimit, submission.MemoryLimit);
                _logger.LogInformation("TestCase - Input: '{Input}', ExpectedOutput: '{ExpectedOutput}'", 
                    cleanInput, cleanExpectedOutput);
                _logger.LogInformation("SourceCode length: {CodeLength} characters", code.Length);

                // Serialize the submission manually to ensure correct format
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };
                var jsonContent = JsonSerializer.Serialize(submission, jsonOptions);
                _logger.LogInformation("Judge0 API request JSON: {JsonContent}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Използваме правилния RapidAPI Judge0 CE endpoint
                var createResponse = await _httpClient.PostAsync(
                    "https://judge0-ce.p.rapidapi.com/submissions?base64_encoded=false&wait=false", content);
                
                // Log the response details
                _logger.LogInformation("Judge0 API response status: {StatusCode}", createResponse.StatusCode);
                
                if (!createResponse.IsSuccessStatusCode)
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Judge0 API error response: {ErrorContent}", errorContent);
                    throw new Exception($"Failed to create submission: {createResponse.StatusCode} - {errorContent}");
                }

                var createResult = await createResponse.Content.ReadFromJsonAsync<Judge0CreateResponse>();
                if (createResult?.Token == null)
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get submission token. Response: {ErrorContent}", errorContent);
                    throw new Exception("Failed to get submission token");
                }

                var token = createResult.Token;
                _logger.LogInformation("Judge0 submission created with token: {Token}", token);
                statusCallback?.Invoke(ExecutionStatus.Running);

                // 2. Изчакване на резултата
                var result = await WaitForResultAsync(token, timeLimit);
                
                var completedAt = DateTime.UtcNow;
                var executionTime = (int)(completedAt - startTime).TotalMilliseconds;

                _logger.LogInformation("Judge0 execution completed - Status: {Status}, Stdout: '{Stdout}', Stderr: '{Stderr}'", 
                    result.Status?.Description, result.Stdout, result.Stderr);

                // 3. Сравняване на резултата с очаквания изход
                var isCorrect = false;
                if (!string.IsNullOrEmpty(result.Stdout) && !string.IsNullOrEmpty(cleanExpectedOutput))
                {
                    // Нормализираме изходите за сравнение
                    var actualOutput = result.Stdout.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
                    var expectedOutput = cleanExpectedOutput.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
                    isCorrect = actualOutput == expectedOutput;
                }

                return new ExecutionResult
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    ActualOutput = result.Stdout,
                    ExecutionTime = executionTime,
                    MemoryUsed = result.Memory,
                    ErrorMessage = result.Stderr,
                    CompilerOutput = result.CompileOutput,
                    RuntimeOutput = result.Stdout,
                    StartedAt = startTime,
                    CompletedAt = completedAt,
                    Status = DetermineStatus(result),
                    IsCorrect = isCorrect,
                    PointsEarned = isCorrect ? testCase.Points : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing code via Judge0 for language: {Language}", language);
                return new ExecutionResult
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    IsCorrect = false,
                    PointsEarned = 0,
                    ErrorMessage = ex.Message,
                    DetailedErrorType = ex.GetType().Name,
                    StackTrace = ex.StackTrace,
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow,
                    Status = ExecutionStatus.SystemError
                };
            }
        }

        public async Task<List<ExecutionResult>> ExecuteAllTestCasesAsync(
            string code,
            string language,
            List<TestCase> testCases,
            int timeLimit,
            int memoryLimit,
            Action<int, int, ExecutionStatus>? progressCallback = null)
        {
            var results = new List<ExecutionResult>();
            
            for (int i = 0; i < testCases.Count; i++)
            {
                var testCase = testCases[i];
                
                Action<ExecutionStatus>? statusCallback = status => 
                    progressCallback?.Invoke(i + 1, testCases.Count, status);
                
                var result = await ExecuteCodeAsync(code, language, testCase, timeLimit, memoryLimit, statusCallback);
                results.Add(result);
                
                // Stop on first compilation error
                if (result.Status == ExecutionStatus.CompilationError)
                {
                    break;
                }
            }
            
            return results;
        }

        private int GetLanguageId(string language) => language.ToLower() switch
        {
            "csharp" => 51,      // C# (.NET Core)
            "python" => 71,      // Python (3.8.1)
            "java" => 62,        // Java (OpenJDK 13.0.1)
            "javascript" => 63,  // JavaScript (Node.js 12.14.0)
            "cpp" => 54,         // C++ (GCC 9.2.0)
            "c" => 50,           // C (GCC 9.2.0)
            "php" => 68,         // PHP (7.4.1)
            "ruby" => 72,        // Ruby (2.7.0)
            "go" => 60,          // Go (1.13.5)
            "rust" => 73,        // Rust (1.40.0)
            "swift" => 83,       // Swift (5.2.3)
            "kotlin" => 78,      // Kotlin (1.3.70)
            "scala" => 81,       // Scala (2.13.2)
            "r" => 80,           // R (4.0.0)
            "dart" => 87,        // Dart (2.7.2)
            _ => throw new ArgumentException($"Unsupported language: {language}")
        };

        private async Task<Judge0Result> WaitForResultAsync(string token, int timeLimit)
        {
            var timeout = TimeSpan.FromSeconds(timeLimit + 10);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                var response = await _httpClient.GetAsync(
                    $"https://judge0-ce.p.rapidapi.com/submissions/{token}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get submission result: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<Judge0Result>();
                if (result?.Status?.Id > 2) // 1=In Queue, 2=Processing, 3=Accepted, 4=Wrong Answer, etc.
                {
                    return result;
                }

                await Task.Delay(500); // Изчакване 500ms преди следваща проверка
            }

            throw new TimeoutException("Code execution timed out");
        }

        private ExecutionStatus DetermineStatus(Judge0Result result)
        {
            return result.Status?.Id switch
            {
                3 => ExecutionStatus.Passed,           // Accepted
                4 => ExecutionStatus.PartiallyCorrect, // Wrong Answer
                5 => ExecutionStatus.TimeLimitExceeded, // Time Limit Exceeded
                6 => ExecutionStatus.CompilationError,  // Compilation Error
                7 => ExecutionStatus.RuntimeError,      // Runtime Error
                8 => ExecutionStatus.MemoryLimitExceeded, // Memory Limit Exceeded
                _ => ExecutionStatus.RuntimeError
            };
        }

        // Judge0 API модели
        private class Judge0Submission
        {
            public string SourceCode { get; set; } = string.Empty;
            public int LanguageId { get; set; }
            public string? Stdin { get; set; }
            public int CpuTimeLimit { get; set; }
            public int MemoryLimit { get; set; }
        }

        private class Judge0CreateResponse
        {
            public string? Token { get; set; }
        }

        private class Judge0Result
        {
            public Judge0Status? Status { get; set; }
            public string? Stdout { get; set; }
            public string? Stderr { get; set; }
            public string? CompileOutput { get; set; }
            public int? Memory { get; set; }
            public double? Time { get; set; }
        }

        private class Judge0Status
        {
            public int Id { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
} 