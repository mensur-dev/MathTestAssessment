using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Examinations.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ExamTasks",
                schema: "dbo",
                newName: "ExamTasks");

            migrationBuilder.RenameTable(
                name: "Exams",
                schema: "dbo",
                newName: "Exams");

            migrationBuilder.CreateTable(
                name: "ProcessedImports",
                schema: "dbo",
                columns: table => new
                {
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedImports", x => x.ImportJobId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedImports",
                schema: "dbo");

            migrationBuilder.RenameTable(
                name: "ExamTasks",
                newName: "ExamTasks",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Exams",
                newName: "Exams",
                newSchema: "dbo");
        }
    }
}
