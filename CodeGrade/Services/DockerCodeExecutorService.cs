using Docker.DotNet;
using Docker.DotNet.Models;
using CodeGrade.Models;
using Microsoft.Extensions.Configuration;

namespace CodeGrade.Services
{
    public class DockerCodeExecutorService : ICodeExecutorService
    {
        private readonly DockerClient _dockerClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DockerCodeExecutorService> _logger;

        public DockerCodeExecutorService(IConfiguration configuration, ILogger<DockerCodeExecutorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Initialize Docker client
            _dockerClient = new DockerClientConfiguration()
                .CreateClient();
        }

        public async Task<ExecutionResult> ExecuteCodeAsync(
            string code, 
            string language, 
            TestCase testCase,
            int timeLimit,
            int memoryLimit)
        {
            try
            {
                var containerName = $"code_exec_{Guid.NewGuid():N}";
                var imageName = GetImageName(language);
                
                // Create container
                var container = await CreateContainerAsync(containerName, imageName, code, testCase.Input, timeLimit, memoryLimit);
                
                // Start container
                await _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                
                // Wait for completion
                var result = await WaitForCompletionAsync(container.ID, timeLimit);
                
                // Get logs
                var logs = await GetContainerLogsAsync(container.ID);
                
                // Clean up
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
                
                return new ExecutionResult
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    ActualOutput = logs.Output,
                    IsCorrect = logs.Output?.Trim() == testCase.ExpectedOutput?.Trim(),
                    PointsEarned = logs.Output?.Trim() == testCase.ExpectedOutput?.Trim() ? testCase.Points : 0,
                    ExecutionTime = logs.ExecutionTime,
                    MemoryUsed = logs.MemoryUsed,
                    ErrorMessage = logs.Error,
                    Status = DetermineStatus(logs, timeLimit, memoryLimit)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing code for language: {Language}", language);
                return new ExecutionResult
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    IsCorrect = false,
                    PointsEarned = 0,
                    ErrorMessage = ex.Message,
                    Status = ExecutionStatus.RuntimeError
                };
            }
        }

        public async Task<List<ExecutionResult>> ExecuteAllTestCasesAsync(
            string code,
            string language,
            List<TestCase> testCases,
            int timeLimit,
            int memoryLimit)
        {
            var results = new List<ExecutionResult>();
            
            foreach (var testCase in testCases)
            {
                var result = await ExecuteCodeAsync(code, language, testCase, timeLimit, memoryLimit);
                results.Add(result);
            }
            
            return results;
        }

        private async Task<ContainerInspectResponse> CreateContainerAsync(
            string containerName, 
            string imageName, 
            string code, 
            string input, 
            int timeLimit, 
            int memoryLimit)
        {
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = containerName,
                Image = imageName,
                Cmd = GetExecutionCommand(code, input),
                WorkingDir = "/workspace",
                HostConfig = new HostConfig
                {
                    Memory = memoryLimit * 1024 * 1024, // Convert MB to bytes
                    MemorySwap = memoryLimit * 1024 * 1024,
                    ReadonlyRootfs = true,
                    SecurityOpt = new[] { "no-new-privileges" }
                },
                NetworkDisabled = true
            });

            return await _dockerClient.Containers.InspectContainerAsync(response.ID);
        }

        private async Task<ContainerExecutionResult> WaitForCompletionAsync(string containerId, int timeLimit)
        {
            var timeout = TimeSpan.FromSeconds(timeLimit + 5); // Add 5 seconds buffer
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
                
                if (container.State.Status == "exited")
                {
                    return new ContainerExecutionResult
                    {
                        ExitCode = (int)container.State.ExitCode,
                        FinishedAt = DateTime.UtcNow
                    };
                }
                
                await Task.Delay(100);
            }
            
            // Timeout - stop container
            await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
            return new ContainerExecutionResult { ExitCode = -1 };
        }

        private async Task<ContainerLogs> GetContainerLogsAsync(string containerId)
        {
            var logs = await _dockerClient.Containers.GetContainerLogsAsync(containerId, new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = false
            });

            var output = new List<string>();
            var error = new List<string>();
            
            using var reader = new StreamReader(logs);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("STDOUT:"))
                {
                    output.Add(line.Substring(7));
                }
                else if (line.StartsWith("STDERR:"))
                {
                    error.Add(line.Substring(7));
                }
            }

            return new ContainerLogs
            {
                Output = string.Join("\n", output),
                Error = string.Join("\n", error)
            };
        }

        private string GetImageName(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => "mcr.microsoft.com/dotnet/sdk:8.0",
                "python" => "python:3.11-slim",
                "java" => "openjdk:17-slim",
                "javascript" => "node:18-slim",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        private string[] GetExecutionCommand(string code, string input)
        {
            // This would need to be implemented based on the specific language
            // For now, return a basic command
            return new[] { "echo", "Not implemented yet" };
        }

        private ExecutionStatus DetermineStatus(ContainerLogs logs, int timeLimit, int memoryLimit)
        {
            if (!string.IsNullOrEmpty(logs.Error))
            {
                return ExecutionStatus.RuntimeError;
            }
            
            // Additional logic for time/memory limits would go here
            return ExecutionStatus.Passed;
        }

        private class ContainerExecutionResult
        {
            public int ExitCode { get; set; }
            public DateTime? FinishedAt { get; set; }
        }

        private class ContainerLogs
        {
            public string? Output { get; set; }
            public string? Error { get; set; }
            public int? ExecutionTime { get; set; }
            public int? MemoryUsed { get; set; }
        }
    }
} 