using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShippingDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Products",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "Products",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Products",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Products",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Products");
        }
    }
}
