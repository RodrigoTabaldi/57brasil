using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductVehicleFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "VehicleFamilies",
                table: "Products",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleFamilies",
                table: "Products");
        }
    }
}
