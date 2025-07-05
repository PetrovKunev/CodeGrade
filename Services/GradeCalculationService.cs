using CodeGrade.Models;
using CodeGrade.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeGrade.Services
{
    public class GradeCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GradeCalculationService> _logger;

        public GradeCalculationService(ApplicationDbContext context, ILogger<GradeCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Grade> CalculateGradeAsync(int submissionId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                throw new ArgumentException($"Submission with ID {submissionId} not found");
            }

            // Calculate total points from execution results
            var totalPointsEarned = submission.ExecutionResults.Sum(er => er.PointsEarned);
            var maxPossiblePoints = submission.Assignment.MaxPoints;

            // Calculate percentage
            var percentage = maxPossiblePoints > 0 ? (double)totalPointsEarned / maxPossiblePoints * 100 : 0;

            // Convert to grade (2-6 scale)
            var gradeValue = CalculateGradeValue(percentage);

            // Create or update grade
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == submission.StudentId && g.AssignmentId == submission.AssignmentId);

            if (existingGrade != null)
            {
                existingGrade.Points = totalPointsEarned;
                existingGrade.GradeValue = gradeValue;
                existingGrade.GradedAt = DateTime.UtcNow;
                
                _context.Grades.Update(existingGrade);
                await _context.SaveChangesAsync();
                
                return existingGrade;
            }
            else
            {
                var grade = new Grade
                {
                    StudentId = submission.StudentId,
                    AssignmentId = submission.AssignmentId,
                    Points = totalPointsEarned,
                    GradeValue = gradeValue,
                    GradedAt = DateTime.UtcNow
                };

                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();
                
                return grade;
            }
        }

        public async Task UpdateSubmissionScoreAsync(int submissionId)
        {
            var submission = await _context.Submissions
                .Include(s => s.ExecutionResults)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                throw new ArgumentException($"Submission with ID {submissionId} not found");
            }

            // Calculate total score from execution results
            var totalScore = submission.ExecutionResults.Sum(er => er.PointsEarned);
            
            // Update submission score
            submission.Score = totalScore;
            submission.Status = SubmissionStatus.Completed;
            
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }

        private int CalculateGradeValue(double percentage)
        {
            return percentage switch
            {
                >= 90 => 6,
                >= 80 => 5,
                >= 70 => 4,
                >= 60 => 3,
                >= 50 => 2,
                _ => 2
            };
        }

        public async Task<List<Grade>> GetStudentGradesAsync(int studentId)
        {
            return await _context.Grades
                .Include(g => g.Assignment)
                .Where(g => g.StudentId == studentId)
                .OrderByDescending(g => g.GradedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, double>> GetStudentStatisticsAsync(int studentId)
        {
            var grades = await GetStudentGradesAsync(studentId);
            
            if (!grades.Any())
            {
                return new Dictionary<string, double>();
            }

            var totalAssignments = grades.Count;
            var averageGrade = grades.Average(g => g.GradeValue ?? 0);
            var averagePoints = grades.Average(g => g.Points);
            var maxPoints = grades.Sum(g => g.Assignment.MaxPoints);
            var totalPointsEarned = grades.Sum(g => g.Points);
            var overallPercentage = maxPoints > 0 ? (totalPointsEarned / (double)maxPoints) * 100 : 0;

            return new Dictionary<string, double>
            {
                ["TotalAssignments"] = totalAssignments,
                ["AverageGrade"] = averageGrade,
                ["AveragePoints"] = averagePoints,
                ["OverallPercentage"] = overallPercentage,
                ["TotalPointsEarned"] = totalPointsEarned,
                ["MaxPossiblePoints"] = maxPoints
            };
        }
    }
} 