using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTestAssessment.Modules.Identity.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenRevocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenRevocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JtiHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRevocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenRevocations_ExpiresAtUtc",
                table: "TokenRevocations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRevocations_JtiHash",
                table: "TokenRevocations",
                column: "JtiHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenRevocations");
        }
    }
}
