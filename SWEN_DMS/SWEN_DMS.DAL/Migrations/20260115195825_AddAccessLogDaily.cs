using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWEN_DMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLogDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessLogsDaily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    AccessCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLogsDaily", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessLogsDaily_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogsDaily_DocumentId",
                table: "AccessLogsDaily",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogsDaily_DocumentId_DayUtc",
                table: "AccessLogsDaily",
                columns: new[] { "DocumentId", "DayUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessLogsDaily");
        }
    }
}
