using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeGrade.Migrations
{
    /// <inheritdoc />
    public partial class AddClassGroupToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassGroupId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ClassGroupId",
                table: "Assignments",
                column: "ClassGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_ClassGroups_ClassGroupId",
                table: "Assignments",
                column: "ClassGroupId",
                principalTable: "ClassGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_ClassGroups_ClassGroupId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_ClassGroupId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ClassGroupId",
                table: "Assignments");
        }
    }
}
