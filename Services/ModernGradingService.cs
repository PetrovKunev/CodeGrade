using CodeGrade.Models;
using CodeGrade.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeGrade.Services
{
    public class ModernGradingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ModernGradingService> _logger;
        private readonly CodeQualityAnalyzer _qualityAnalyzer;

        public ModernGradingService(
            ApplicationDbContext context, 
            ILogger<ModernGradingService> logger,
            CodeQualityAnalyzer qualityAnalyzer)
        {
            _context = context;
            _logger = logger;
            _qualityAnalyzer = qualityAnalyzer;
        }

        public async Task<GradeResult> CalculateModernGradeAsync(int submissionId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.AssessmentCriteria)
                .Include(s => s.Assignment.TestCases)
                .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                throw new ArgumentException($"Submission with ID {submissionId} not found");
            }

            var criteria = submission.Assignment.AssessmentCriteria ?? await GetDefaultCriteriaAsync();
            
            // Calculate multi-dimensional scores
            var correctnessScore = await CalculateCorrectnessScoreAsync(submission);
            var qualityScore = await CalculateQualityScoreAsync(submission);
            var testingScore = await CalculateTestingScoreAsync(submission);
            var approachScore = await CalculateApproachScoreAsync(submission);

            // Calculate weighted final score
            var finalScore = CalculateWeightedScore(
                correctnessScore, qualityScore, testingScore, approachScore, criteria);

            // Convert to traditional grade
            var gradeValue = ConvertToTraditionalGrade(finalScore);

            // Check for improvement
            var (attemptNumber, previousScore, isImprovement) = await CalculateProgressAsync(submission);

            // Generate feedback
            var feedback = await GenerateFeedbackAsync(submission, correctnessScore, qualityScore, testingScore, approachScore);

            var gradeResult = new GradeResult
            {
                SubmissionId = submissionId,
                CorrectnessScore = correctnessScore,
                QualityScore = qualityScore,
                TestingScore = testingScore,
                ApproachScore = approachScore,
                FinalScore = finalScore,
                GradeValue = gradeValue,
                CorrectnessFeedback = feedback.CorrectnessFeedback,
                QualityFeedback = feedback.QualityFeedback,
                TestingFeedback = feedback.TestingFeedback,
                ApproachFeedback = feedback.ApproachFeedback,
                OverallFeedback = feedback.OverallFeedback,
                AttemptNumber = attemptNumber,
                PreviousScore = previousScore,
                IsImprovement = isImprovement,
                AssessmentCriteriaId = criteria.Id,
                CalculatedBy = "System"
            };

            _context.GradeResults.Add(gradeResult);
            await _context.SaveChangesAsync();

            return gradeResult;
        }

        private async Task<int> CalculateCorrectnessScoreAsync(Submission submission)
        {
            var totalPoints = submission.Assignment.TestCases.Sum(tc => tc.Points);
            if (totalPoints == 0) return 0;

            var earnedPoints = submission.ExecutionResults.Sum(er => er.PointsEarned);
            return (int)Math.Round((double)earnedPoints / totalPoints * 100);
        }

        private async Task<int> CalculateQualityScoreAsync(Submission submission)
        {
            // Analyze code quality using the quality analyzer
            var qualityMetrics = await _qualityAnalyzer.AnalyzeCodeQualityAsync(submission.Code, submission.Language);
            
            // Calculate composite quality score
            var readabilityWeight = 0.4;
            var maintainabilityWeight = 0.3;
            var styleWeight = 0.2;
            var documentationWeight = 0.1;

            var qualityScore = (int)Math.Round(
                qualityMetrics.ReadabilityScore * readabilityWeight +
                qualityMetrics.MaintainabilityIndex * maintainabilityWeight +
                qualityMetrics.StyleScore * styleWeight +
                qualityMetrics.DocumentationCoverage * documentationWeight
            );

            // Store quality metrics
            _context.QualityMetrics.Add(qualityMetrics);

            return Math.Min(100, Math.Max(0, qualityScore));
        }

        private async Task<int> CalculateTestingScoreAsync(Submission submission)
        {
            // For now, based on execution results diversity and edge case coverage
            var totalTestCases = submission.Assignment.TestCases.Count;
            if (totalTestCases == 0) return 100;

            var passedTestCases = submission.ExecutionResults.Count(er => er.IsCorrect);
            var edgeCasesPassed = submission.ExecutionResults
                .Where(er => er.IsCorrect && er.TestCase.IsHidden)
                .Count();

            var basicScore = (int)Math.Round((double)passedTestCases / totalTestCases * 70);
            var edgeCaseBonus = Math.Min(30, edgeCasesPassed * 10);

            return Math.Min(100, basicScore + edgeCaseBonus);
        }

        private async Task<int> CalculateApproachScoreAsync(Submission submission)
        {
            // Basic approach scoring - can be enhanced with AI analysis
            var codeLength = submission.Code.Length;
            var complexity = _qualityAnalyzer.CalculateComplexity(submission.Code);
            
            // Reward concise, clear solutions
            var efficiencyScore = codeLength < 1000 ? 30 : Math.Max(10, 30 - (codeLength - 1000) / 100);
            var simplicityScore = complexity < 10 ? 30 : Math.Max(10, 30 - (complexity - 10) * 2);
            var innovationScore = 40; // Base score, can be enhanced with pattern recognition

            return Math.Min(100, efficiencyScore + simplicityScore + innovationScore);
        }

        private int CalculateWeightedScore(int correctness, int quality, int testing, int approach, AssessmentCriteria criteria)
        {
            return (int)Math.Round(
                correctness * criteria.CorrectnessWeight / 100.0 +
                quality * criteria.QualityWeight / 100.0 +
                testing * criteria.TestingWeight / 100.0 +
                approach * criteria.ApproachWeight / 100.0
            );
        }

        private int ConvertToTraditionalGrade(int finalScore)
        {
            return finalScore switch
            {
                >= 90 => 6,
                >= 80 => 5,
                >= 70 => 4,
                >= 60 => 3,
                >= 50 => 2,
                _ => 2
            };
        }

        private async Task<(int attemptNumber, int? previousScore, bool isImprovement)> CalculateProgressAsync(Submission submission)
        {
            var previousGrades = await _context.GradeResults
                .Where(gr => gr.Submission.StudentId == submission.StudentId && 
                           gr.Submission.AssignmentId == submission.AssignmentId &&
                           gr.SubmissionId != submission.Id)
                .OrderByDescending(gr => gr.CalculatedAt)
                .ToListAsync();

            var attemptNumber = previousGrades.Count + 1;
            var previousScore = previousGrades.FirstOrDefault()?.FinalScore;
            var isImprovement = previousScore.HasValue && previousScore < submission.Score;

            return (attemptNumber, previousScore, isImprovement);
        }

        private async Task<FeedbackResult> GenerateFeedbackAsync(Submission submission, int correctness, int quality, int testing, int approach)
        {
            var feedback = new FeedbackResult();

            // Correctness feedback
            var passedTests = submission.ExecutionResults.Count(er => er.IsCorrect);
            var totalTests = submission.ExecutionResults.Count;
            feedback.CorrectnessFeedback = $"Passed {passedTests}/{totalTests} test cases. ";
            
            if (correctness >= 90)
                feedback.CorrectnessFeedback += "Excellent correctness!";
            else if (correctness >= 70)
                feedback.CorrectnessFeedback += "Good correctness, minor issues to address.";
            else
                feedback.CorrectnessFeedback += "Several test cases failed. Review the logic carefully.";

            // Quality feedback
            feedback.QualityFeedback = quality switch
            {
                >= 90 => "Excellent code quality with clear structure and good practices.",
                >= 70 => "Good code quality with room for improvement in naming or documentation.",
                >= 50 => "Acceptable code quality but needs improvement in readability.",
                _ => "Code quality needs significant improvement. Focus on structure and clarity."
            };

            // Testing feedback
            feedback.TestingFeedback = testing switch
            {
                >= 90 => "Excellent test coverage including edge cases.",
                >= 70 => "Good test coverage with most scenarios handled.",
                _ => "Test coverage could be improved. Consider more edge cases."
            };

            // Approach feedback
            feedback.ApproachFeedback = approach switch
            {
                >= 90 => "Excellent problem-solving approach with efficient implementation.",
                >= 70 => "Good approach with minor optimization opportunities.",
                _ => "Consider reviewing your approach for better efficiency or clarity."
            };

            // Overall feedback
            var overallScore = (correctness + quality + testing + approach) / 4;
            feedback.OverallFeedback = $"Overall performance: {overallScore}%. ";
            
            if (overallScore >= 90)
                feedback.OverallFeedback += "Outstanding work! Keep it up!";
            else if (overallScore >= 70)
                feedback.OverallFeedback += "Good work! Continue improving code quality.";
            else
                feedback.OverallFeedback += "Keep practicing. Focus on correctness first, then quality.";

            return feedback;
        }

        private async Task<AssessmentCriteria> GetDefaultCriteriaAsync()
        {
            var defaultCriteria = await _context.AssessmentCriteria
                .FirstOrDefaultAsync(ac => ac.Name == "Default");
                
            if (defaultCriteria == null)
            {
                defaultCriteria = new AssessmentCriteria
                {
                    Name = "Default",
                    Description = "Default assessment criteria for general programming assignments",
                    Level = DifficultyLevel.Intermediate,
                    CorrectnessWeight = 50,
                    QualityWeight = 30,
                    TestingWeight = 15,
                    ApproachWeight = 5,
                    CreatedBy = "System"
                };
                
                _context.AssessmentCriteria.Add(defaultCriteria);
                await _context.SaveChangesAsync();
            }
            
            return defaultCriteria;
        }

        private class FeedbackResult
        {
            public string CorrectnessFeedback { get; set; } = string.Empty;
            public string QualityFeedback { get; set; } = string.Empty;
            public string TestingFeedback { get; set; } = string.Empty;
            public string ApproachFeedback { get; set; } = string.Empty;
            public string OverallFeedback { get; set; } = string.Empty;
        }
    }
}