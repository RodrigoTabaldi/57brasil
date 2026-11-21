using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductSourceReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompatibilitySourceReference",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceSourceReference",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockSourceReference",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceSnapshot",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true);

            // Existing offers have no recorded evidence. Keep them visible as catalog drafts.
            migrationBuilder.Sql("UPDATE \"Products\" SET \"Published\" = FALSE, \"CatalogVisible\" = TRUE WHERE \"Published\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompatibilitySourceReference",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceSourceReference",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockSourceReference",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EvidenceSnapshot",
                table: "AuditLogs");
        }
    }
}
