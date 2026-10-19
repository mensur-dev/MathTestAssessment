using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Examinations.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialExaminations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Exams",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalExamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamTasks",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalTaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SubmittedAnswer = table.Column<decimal>(type: "decimal(38,18)", precision: 38, scale: 18, nullable: false),
                    CorrectAnswer = table.Column<decimal>(type: "decimal(38,18)", precision: 38, scale: 18, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    EvaluationErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamTasks_Exams_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "dbo",
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_StudentId",
                schema: "dbo",
                table: "Exams",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TeacherId",
                schema: "dbo",
                table: "Exams",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTasks_ExamId_ExternalTaskId",
                schema: "dbo",
                table: "ExamTasks",
                columns: new[] { "ExamId", "ExternalTaskId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamTasks",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Exams",
                schema: "dbo");
        }
    }
}
