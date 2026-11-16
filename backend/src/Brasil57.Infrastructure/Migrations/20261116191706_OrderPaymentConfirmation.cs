using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderPaymentConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentId",
                table: "Orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProviderPaymentId",
                table: "Orders",
                column: "ProviderPaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ProviderPaymentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentId",
                table: "Orders");
        }
    }
}
