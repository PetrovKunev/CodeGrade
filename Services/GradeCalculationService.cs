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

            // Намери най-добрия резултат за тази задача от всички решения
            var bestSubmission = await _context.Submissions
                .Include(s => s.ExecutionResults)
                .ThenInclude(er => er.TestCase)
                .Where(s => s.StudentId == submission.StudentId && 
                           s.AssignmentId == submission.AssignmentId &&
                           s.Status == SubmissionStatus.Completed)
                .OrderByDescending(s => s.ExecutionResults.Sum(er => er.PointsEarned))
                .FirstOrDefaultAsync();

            if (bestSubmission == null)
            {
                // Ако няма завършени решения, не създавай оценка
                return null;
            }

            // Изчисли оценката от най-добрия резултат
            var totalPointsEarned = bestSubmission.ExecutionResults.Sum(er => er.PointsEarned);
            var maxPossiblePoints = bestSubmission.Assignment.MaxPoints;

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
                >= 90 => 6,    // Отличен
                >= 80 => 5,    // Много добър
                >= 70 => 4,    // Добър
                >= 60 => 3,    // Среден
                >= 50 => 2,    // Слаб
                _ => 2         // Слаб (по подразбиране)
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

            var totalAssignments = grades.Count;
            var completedAssignments = grades.Count; // Всички оценки представляват завършени задачи
            var averageGrade = grades.Any() ? grades.Average(g => g.GradeValue ?? 0) : 0;
            var bestGrade = grades.Any() ? grades.Max(g => g.GradeValue ?? 0) : 0;

            return new Dictionary<string, double>
            {
                ["TotalAssignments"] = totalAssignments,
                ["CompletedAssignments"] = completedAssignments,
                ["AverageGrade"] = averageGrade,
                ["BestGrade"] = bestGrade
            };
        }
    }
} 