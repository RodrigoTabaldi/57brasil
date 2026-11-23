using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartTypePurchaseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartTypePurchaseRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartTypePurchaseRequests", x => x.Id);
                    table.CheckConstraint("ck_part_type_purchase_request_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 99");
                    table.ForeignKey(
                        name: "FK_PartTypePurchaseRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartTypePurchaseRequests_PartTypes_PartTypeId",
                        column: x => x.PartTypeId,
                        principalTable: "PartTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartTypePurchaseRequests_CreatedAt",
                table: "PartTypePurchaseRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PartTypePurchaseRequests_PartTypeId",
                table: "PartTypePurchaseRequests",
                column: "PartTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartTypePurchaseRequests_UserId_PartTypeId_CreatedAt",
                table: "PartTypePurchaseRequests",
                columns: new[] { "UserId", "PartTypeId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartTypePurchaseRequests");
        }
    }
}
