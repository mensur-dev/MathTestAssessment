using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Students.Infrastructure.Data.Migrations;

public partial class RenameXmlIdentifierToId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.RenameColumn(name: "Id", table: "Students", newName: "InternalId");
            migrationBuilder.RenameColumn(name: "ExternalStudentId", table: "Students", newName: "ID");
            migrationBuilder.RenameIndex(name: "IX_Students_ExternalStudentId", table: "Students", newName: "IX_Students_ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.RenameColumn(name: "ID", table: "Students", newName: "ExternalStudentId");
            migrationBuilder.RenameColumn(name: "InternalId", table: "Students", newName: "Id");
            migrationBuilder.RenameIndex(name: "IX_Students_ID", table: "Students", newName: "IX_Students_ExternalStudentId");
    }
}
