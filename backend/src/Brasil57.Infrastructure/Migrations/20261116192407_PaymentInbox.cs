using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotifications_ProcessedAt_NextAttemptAt",
                table: "PaymentNotifications",
                columns: new[] { "ProcessedAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotifications_RequestId",
                table: "PaymentNotifications",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentNotifications");
        }
    }
}
