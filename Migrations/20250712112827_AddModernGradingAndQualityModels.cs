using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeGrade.Migrations
{
    /// <inheritdoc />
    public partial class AddModernGradingAndQualityModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_ClassGroupId",
                table: "Students");

            migrationBuilder.AddColumn<string>(
                name: "CompilerOutput",
                table: "ExecutionResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ExecutionResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailedErrorType",
                table: "ExecutionResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiffOutput",
                table: "ExecutionResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "ExecutionResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OutputCaseInsensitiveMatches",
                table: "ExecutionResults",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OutputTrimMatches",
                table: "ExecutionResults",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuntimeOutput",
                table: "ExecutionResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StackTrace",
                table: "ExecutionResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ExecutionResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentCriteriaId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CorrectnessWeight = table.Column<int>(type: "int", nullable: false),
                    QualityWeight = table.Column<int>(type: "int", nullable: false),
                    TestingWeight = table.Column<int>(type: "int", nullable: false),
                    ApproachWeight = table.Column<int>(type: "int", nullable: false),
                    MinReadabilityScore = table.Column<int>(type: "int", nullable: false),
                    MinMaintainabilityScore = table.Column<int>(type: "int", nullable: false),
                    MinDocumentationScore = table.Column<int>(type: "int", nullable: false),
                    PassThreshold = table.Column<int>(type: "int", nullable: false),
                    ExcellenceThreshold = table.Column<int>(type: "int", nullable: false),
                    AllowUnlimitedResubmissions = table.Column<bool>(type: "bit", nullable: false),
                    TrackImprovement = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    CyclomaticComplexity = table.Column<int>(type: "int", nullable: false),
                    LinesOfCode = table.Column<int>(type: "int", nullable: false),
                    MethodCount = table.Column<int>(type: "int", nullable: false),
                    ClassCount = table.Column<int>(type: "int", nullable: false),
                    ReadabilityScore = table.Column<int>(type: "int", nullable: false),
                    NamingScore = table.Column<int>(type: "int", nullable: false),
                    StructureScore = table.Column<int>(type: "int", nullable: false),
                    DocumentationCoverage = table.Column<int>(type: "int", nullable: false),
                    CommentQuality = table.Column<int>(type: "int", nullable: false),
                    StyleScore = table.Column<int>(type: "int", nullable: false),
                    StyleViolations = table.Column<int>(type: "int", nullable: false),
                    MaintainabilityIndex = table.Column<int>(type: "int", nullable: false),
                    TechnicalDebt = table.Column<double>(type: "float", nullable: false),
                    SecurityIssues = table.Column<int>(type: "int", nullable: false),
                    SecurityFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    PerformanceFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementSuggestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalysisVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToolsUsed = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityMetrics_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    CorrectnessScore = table.Column<int>(type: "int", nullable: false),
                    QualityScore = table.Column<int>(type: "int", nullable: false),
                    TestingScore = table.Column<int>(type: "int", nullable: false),
                    ApproachScore = table.Column<int>(type: "int", nullable: false),
                    FinalScore = table.Column<int>(type: "int", nullable: false),
                    GradeValue = table.Column<int>(type: "int", nullable: true),
                    CorrectnessFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestingFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproachFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    PreviousScore = table.Column<int>(type: "int", nullable: true),
                    IsImprovement = table.Column<bool>(type: "bit", nullable: false),
                    AssessmentCriteriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeResults_AssessmentCriteria_AssessmentCriteriaId",
                        column: x => x.AssessmentCriteriaId,
                        principalTable: "AssessmentCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeResults_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassGroupId_ClassNumber",
                table: "Students",
                columns: new[] { "ClassGroupId", "ClassNumber" },
                unique: true,
                filter: "[ClassGroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssessmentCriteriaId",
                table: "Assignments",
                column: "AssessmentCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_AssessmentCriteriaId",
                table: "GradeResults",
                column: "AssessmentCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_SubmissionId",
                table: "GradeResults",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityMetrics_SubmissionId",
                table: "QualityMetrics",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AssessmentCriteria_AssessmentCriteriaId",
                table: "Assignments",
                column: "AssessmentCriteriaId",
                principalTable: "AssessmentCriteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AssessmentCriteria_AssessmentCriteriaId",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "GradeResults");

            migrationBuilder.DropTable(
                name: "QualityMetrics");

            migrationBuilder.DropTable(
                name: "AssessmentCriteria");

            migrationBuilder.DropIndex(
                name: "IX_Students_ClassGroupId_ClassNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssessmentCriteriaId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CompilerOutput",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "DetailedErrorType",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "DiffOutput",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "OutputCaseInsensitiveMatches",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "OutputTrimMatches",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "RuntimeOutput",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "StackTrace",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ExecutionResults");

            migrationBuilder.DropColumn(
                name: "AssessmentCriteriaId",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassGroupId",
                table: "Students",
                column: "ClassGroupId");
        }
    }
}
