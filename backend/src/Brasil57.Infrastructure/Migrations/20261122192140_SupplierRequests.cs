using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Contact = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Street = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Complement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Vehicle = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierRequests_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierRequestItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ExactPart = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Available = table.Column<bool>(type: "boolean", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    LeadDays = table.Column<int>(type: "integer", nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: true),
                    HeightCm = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    WidthCm = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    LengthCm = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierRequestItems", x => x.Id);
                    table.CheckConstraint("ck_supplier_request_item", "\"Quantity\" BETWEEN 1 AND 99 AND (\"UnitPrice\" IS NULL OR \"UnitPrice\" >= 0)");
                    table.ForeignKey(
                        name: "FK_SupplierRequestItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierRequestItems_SupplierRequests_SupplierRequestId",
                        column: x => x.SupplierRequestId,
                        principalTable: "SupplierRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRequestItems_ProductId",
                table: "SupplierRequestItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRequestItems_SupplierRequestId",
                table: "SupplierRequestItems",
                column: "SupplierRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRequests_CreatedAt",
                table: "SupplierRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRequests_CustomerId_IdempotencyKey",
                table: "SupplierRequests",
                columns: new[] { "CustomerId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierRequestItems");

            migrationBuilder.DropTable(
                name: "SupplierRequests");
        }
    }
}
