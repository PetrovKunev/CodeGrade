using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CodeGrade.Models;

namespace CodeGrade.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<ClassGroup> ClassGroups { get; set; }
        public DbSet<SubjectModule> SubjectModules { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ExecutionResult> ExecutionResults { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<GradeResult> GradeResults { get; set; }
        public DbSet<QualityMetrics> QualityMetrics { get; set; }
        public DbSet<AssessmentCriteria> AssessmentCriteria { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Student>()
                .HasOne(s => s.ClassGroup)
                .WithMany(cg => cg.Students)
                .HasForeignKey(s => s.ClassGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Assignment>()
                .HasOne(a => a.SubjectModule)
                .WithMany(sm => sm.Assignments)
                .HasForeignKey(a => a.SubjectModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany(t => t.Assignments)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TestCase>()
                .HasOne(tc => tc.Assignment)
                .WithMany(a => a.TestCases)
                .HasForeignKey(tc => tc.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExecutionResult>()
                .HasOne(er => er.Submission)
                .WithMany(s => s.ExecutionResults)
                .HasForeignKey(er => er.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExecutionResult>()
                .HasOne(er => er.TestCase)
                .WithMany()
                .HasForeignKey(er => er.TestCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Grade>()
                .HasOne(g => g.Assignment)
                .WithMany()
                .HasForeignKey(g => g.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GradeValue precision for decimal type
            builder.Entity<Grade>()
                .Property(g => g.GradeValue)
                .HasPrecision(3, 1); // 3 total digits, 1 decimal place (e.g., 6.0)

            // Configure indexes
            builder.Entity<Student>()
                .HasIndex(s => s.StudentNumber)
                .IsUnique();

            builder.Entity<Student>()
                .HasIndex(s => new { s.ClassGroupId, s.ClassNumber })
                .IsUnique();

            builder.Entity<Submission>()
                .HasIndex(s => new { s.StudentId, s.AssignmentId, s.SubmittedAt });

            builder.Entity<Grade>()
                .HasIndex(g => new { g.StudentId, g.AssignmentId })
                .IsUnique();

            // Configure GradeResult relationships
            builder.Entity<GradeResult>()
                .HasOne(gr => gr.Submission)
                .WithMany()
                .HasForeignKey(gr => gr.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GradeResult>()
                .HasOne(gr => gr.AssessmentCriteria)
                .WithMany(ac => ac.GradeResults)
                .HasForeignKey(gr => gr.AssessmentCriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure QualityMetrics relationships
            builder.Entity<QualityMetrics>()
                .HasOne(qm => qm.Submission)
                .WithMany()
                .HasForeignKey(qm => qm.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AssessmentCriteria relationships
            builder.Entity<AssessmentCriteria>()
                .HasMany(ac => ac.Assignments)
                .WithOne(a => a.AssessmentCriteria)
                .HasForeignKey(a => a.AssessmentCriteriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 