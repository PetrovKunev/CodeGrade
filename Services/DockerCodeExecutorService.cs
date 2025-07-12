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
            int memoryLimit,
            Action<ExecutionStatus>? statusCallback = null)
        {
            var startTime = DateTime.UtcNow;
            statusCallback?.Invoke(ExecutionStatus.Queued);
            
            try
            {
                var containerName = $"code_exec_{Guid.NewGuid():N}";
                var imageName = GetImageName(language);
                
                statusCallback?.Invoke(ExecutionStatus.Compiling);
                
                // Create container
                var container = await CreateContainerAsync(containerName, imageName, code, testCase.Input, timeLimit, memoryLimit, language);
                
                statusCallback?.Invoke(ExecutionStatus.Running);
                
                // Start container
                await _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                
                // Wait for completion
                var containerResult = await WaitForCompletionAsync(container.ID, timeLimit);
                
                // Get logs
                var logs = await GetContainerLogsAsync(container.ID);
                
                // Get container stats
                var stats = await GetContainerStatsAsync(container.ID);
                
                // Clean up
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
                
                var completedAt = DateTime.UtcNow;
                var executionTime = (int)(completedAt - startTime).TotalMilliseconds;
                
                var result = new ExecutionResult
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    ActualOutput = logs.Output,
                    ExecutionTime = executionTime,
                    MemoryUsed = stats.MemoryUsed,
                    ErrorMessage = logs.Error,
                    CompilerOutput = logs.CompilerOutput,
                    RuntimeOutput = logs.RuntimeOutput,
                    StartedAt = startTime,
                    CompletedAt = completedAt,
                    Status = DetermineStatus(logs, containerResult, timeLimit, memoryLimit)
                };
                
                // Detailed comparison analysis
                AnalyzeOutput(result, testCase);
                
                return result;
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

        private async Task<ContainerInspectResponse> CreateContainerAsync(
            string containerName, 
            string imageName, 
            string code, 
            string input, 
            int timeLimit, 
            int memoryLimit,
            string language)
        {
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = containerName,
                Image = imageName,
                Cmd = GetExecutionCommand(code, input, language),
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
            
            // Docker logs contain binary stream data - need to parse properly
            using var memoryStream = new MemoryStream();
            await logs.CopyToAsync(memoryStream);
            var logData = memoryStream.ToArray();
            
            // Parse Docker multiplexed stream format
            var (stdout, stderr) = ParseDockerLogs(logData);
            
            if (!string.IsNullOrEmpty(stdout))
            {
                output.AddRange(stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            }
            
            if (!string.IsNullOrEmpty(stderr))
            {
                error.AddRange(stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            }

            return new ContainerLogs
            {
                Output = string.Join("\n", output),
                Error = string.Join("\n", error),
                CompilerOutput = string.Join("\n", error), // Compiler errors typically go to stderr
                RuntimeOutput = string.Join("\n", output)   // Program output goes to stdout
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

        private string[] GetExecutionCommand(string code, string input, string language)
        {
            var escapedCode = code.Replace("'", "'\"'\"'");
            var escapedInput = input?.Replace("'", "'\"'\"'") ?? "";
            
            return language.ToLower() switch
            {
                "csharp" => new[] { "/bin/bash", "/workspace/scripts/run-csharp.sh", escapedCode, escapedInput },
                "python" => new[] { "/bin/bash", "/workspace/scripts/run-python.sh", escapedCode, escapedInput },
                "java" => new[] { "/bin/bash", "/workspace/scripts/run-java.sh", escapedCode, escapedInput },
                "javascript" => new[] { "/bin/bash", "/workspace/scripts/run-javascript.sh", escapedCode, escapedInput },
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        private void AnalyzeOutput(ExecutionResult result, TestCase testCase)
        {
            if (string.IsNullOrEmpty(result.ActualOutput) || string.IsNullOrEmpty(testCase.ExpectedOutput))
            {
                result.IsCorrect = false;
                result.PointsEarned = 0;
                return;
            }
            
            var actual = result.ActualOutput;
            var expected = testCase.ExpectedOutput;
            
            // Exact match
            result.IsCorrect = actual == expected;
            
            // Trimmed match
            result.OutputTrimMatches = actual.Trim() == expected.Trim();
            
            // Case insensitive match
            result.OutputCaseInsensitiveMatches = string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
            
            // Generate diff for better visualization
            result.DiffOutput = GenerateDiff(expected, actual);
            
            // Award points based on correctness
            if (result.IsCorrect || result.OutputTrimMatches == true)
            {
                result.PointsEarned = testCase.Points;
            }
            else if (result.OutputCaseInsensitiveMatches == true)
            {
                result.PointsEarned = (int)(testCase.Points * 0.8); // 80% for case mismatch
                result.Status = ExecutionStatus.PartiallyCorrect;
            }
            else
            {
                result.PointsEarned = 0;
            }
        }
        
        private string GenerateDiff(string expected, string actual)
        {
            var expectedLines = expected.Split('\n');
            var actualLines = actual.Split('\n');
            
            var diff = new System.Text.StringBuilder();
            var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                var expectedLine = i < expectedLines.Length ? expectedLines[i] : "";
                var actualLine = i < actualLines.Length ? actualLines[i] : "";
                
                if (expectedLine != actualLine)
                {
                    diff.AppendLine($"Line {i + 1}:");
                    diff.AppendLine($"  Expected: {expectedLine}");
                    diff.AppendLine($"  Actual:   {actualLine}");
                }
            }
            
            return diff.ToString();
        }
        
        private async Task<ContainerStats> GetContainerStatsAsync(string containerId)
        {
            try
            {
                var statsStream = await _dockerClient.Containers.GetContainerStatsAsync(containerId, new ContainerStatsParameters
                {
                    OneShot = true,
                    Stream = false
                }, CancellationToken.None);
                
                // Read the stats JSON
                using var reader = new StreamReader(statsStream);
                var statsJson = await reader.ReadToEndAsync();
                
                if (string.IsNullOrEmpty(statsJson))
                {
                    return new ContainerStats { MemoryUsed = 0 };
                }
                
                try
                {
                    // Parse Docker stats JSON to extract memory usage
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(statsJson);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("memory_stats", out var memoryStats) &&
                        memoryStats.TryGetProperty("usage", out var usage))
                    {
                        var memoryBytes = usage.GetInt64();
                        var memoryKB = (int)(memoryBytes / 1024); // Convert to KB
                        return new ContainerStats { MemoryUsed = memoryKB };
                    }
                }
                catch
                {
                    // If JSON parsing fails, return 0
                }
                
                return new ContainerStats { MemoryUsed = 0 };
            }
            catch
            {
                return new ContainerStats { MemoryUsed = 0 };
            }
        }
        
        private ExecutionStatus DetermineStatus(ContainerLogs logs, ContainerExecutionResult containerResult, int timeLimit, int memoryLimit)
        {
            // Check for compilation errors first
            if (!string.IsNullOrEmpty(logs.CompilerOutput) && logs.CompilerOutput.Contains("error"))
            {
                return ExecutionStatus.CompilationError;
            }
            
            // Check exit code
            if (containerResult.ExitCode != 0)
            {
                if (containerResult.ExitCode == 124) // timeout exit code
                {
                    return ExecutionStatus.TimeLimitExceeded;
                }
                return ExecutionStatus.RuntimeError;
            }
            
            // Check for runtime errors
            if (!string.IsNullOrEmpty(logs.Error))
            {
                if (logs.Error.Contains("timeout") || logs.Error.Contains("killed"))
                {
                    return ExecutionStatus.TimeLimitExceeded;
                }
                if (logs.Error.Contains("memory") || logs.Error.Contains("oom"))
                {
                    return ExecutionStatus.MemoryLimitExceeded;
                }
                return ExecutionStatus.RuntimeError;
            }
            
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
            public string? CompilerOutput { get; set; }
            public string? RuntimeOutput { get; set; }
            public int? ExecutionTime { get; set; }
            public int? MemoryUsed { get; set; }
        }
        
        private (string stdout, string stderr) ParseDockerLogs(byte[] logData)
        {
            var stdout = new List<byte>();
            var stderr = new List<byte>();
            
            int index = 0;
            while (index < logData.Length)
            {
                if (index + 8 > logData.Length) break;
                
                // Docker multiplexed stream format:
                // [stream_type][0][0][0][size1][size2][size3][size4][payload]
                byte streamType = logData[index];
                
                // Get payload size (big-endian)
                int payloadSize = (logData[index + 4] << 24) |
                                (logData[index + 5] << 16) |
                                (logData[index + 6] << 8) |
                                logData[index + 7];
                
                index += 8; // Skip header
                
                if (index + payloadSize > logData.Length) break;
                
                var payload = new byte[payloadSize];
                Array.Copy(logData, index, payload, 0, payloadSize);
                
                // Stream type: 1 = stdout, 2 = stderr
                if (streamType == 1)
                {
                    stdout.AddRange(payload);
                }
                else if (streamType == 2)
                {
                    stderr.AddRange(payload);
                }
                
                index += payloadSize;
            }
            
            return (System.Text.Encoding.UTF8.GetString(stdout.ToArray()),
                   System.Text.Encoding.UTF8.GetString(stderr.ToArray()));
        }

        private class ContainerStats
        {
            public int? MemoryUsed { get; set; }
        }
    }
} 