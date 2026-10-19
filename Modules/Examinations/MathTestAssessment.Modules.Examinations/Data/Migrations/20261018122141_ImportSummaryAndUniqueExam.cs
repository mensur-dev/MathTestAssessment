using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Examinations.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImportSummaryAndUniqueExam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SummaryJson",
                schema: "dbo",
                table: "ProcessedImports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TeacherId_StudentId_ExternalExamId",
                table: "Exams",
                columns: new[] { "TeacherId", "StudentId", "ExternalExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Exams_TeacherId_StudentId_ExternalExamId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SummaryJson",
                schema: "dbo",
                table: "ProcessedImports");
        }
    }
}
