using CodeGrade.Models;

namespace CodeGrade.Services
{
    public interface ICodeExecutorService
    {
        Task<ExecutionResult> ExecuteCodeAsync(
            string code, 
            string language, 
            TestCase testCase,
            int timeLimit,
            int memoryLimit,
            Action<ExecutionStatus>? statusCallback = null);
            
        Task<List<ExecutionResult>> ExecuteAllTestCasesAsync(
            string code,
            string language,
            List<TestCase> testCases,
            int timeLimit,
            int memoryLimit,
            Action<int, int, ExecutionStatus>? progressCallback = null);
    }
} 