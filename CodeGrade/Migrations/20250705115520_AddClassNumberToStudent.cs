using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeGrade.Migrations
{
    /// <inheritdoc />
    public partial class AddClassNumberToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassNumber",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassNumber",
                table: "Students");
        }
    }
}
