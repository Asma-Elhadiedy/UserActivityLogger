using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserActivityLogger.Tests.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivityLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Path = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateEvent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_DateEvent",
                table: "UserActivityLog",
                column: "DateEvent");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_Event",
                table: "UserActivityLog",
                column: "Event");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_UserId",
                table: "UserActivityLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_UserId_DateEvent",
                table: "UserActivityLog",
                columns: new[] { "UserId", "DateEvent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityLog");
        }
    }
}
