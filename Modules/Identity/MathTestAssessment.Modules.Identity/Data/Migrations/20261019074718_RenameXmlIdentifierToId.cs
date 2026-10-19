using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Identity.Data.Migrations;

public partial class RenameXmlIdentifierToId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropIndex(name: "IX_AspNetUsers_ExternalTeacherId", table: "AspNetUsers");
            migrationBuilder.RenameColumn(name: "Id", table: "AspNetUsers", newName: "InternalId");
            migrationBuilder.RenameColumn(name: "ExternalTeacherId", table: "AspNetUsers", newName: "ID");
            migrationBuilder.CreateIndex(name: "IX_AspNetUsers_ID", table: "AspNetUsers", column: "ID", unique: true, filter: "[ID] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropIndex(name: "IX_AspNetUsers_ID", table: "AspNetUsers");
            migrationBuilder.RenameColumn(name: "ID", table: "AspNetUsers", newName: "ExternalTeacherId");
            migrationBuilder.RenameColumn(name: "InternalId", table: "AspNetUsers", newName: "Id");
            migrationBuilder.CreateIndex(name: "IX_AspNetUsers_ExternalTeacherId", table: "AspNetUsers", column: "ExternalTeacherId", unique: true, filter: "[ExternalTeacherId] IS NOT NULL");
    }
}
