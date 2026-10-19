using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.BackgroundJobs.Migrations
{
    /// <inheritdoc />
    public partial class AddImportFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "dbo",
                table: "ImportJobs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "dbo",
                table: "ImportJobs");
        }
    }
}
