using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brasil57.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommercialProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommercialSource",
                table: "Products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CommercialVerifiedOn",
                table: "Products",
                type: "date",
                nullable: true);

            // Earlier publications have no recorded source and require review before sale.
            migrationBuilder.Sql("UPDATE \"Products\" SET \"Published\" = FALSE WHERE \"Published\" = TRUE");

            migrationBuilder.AddCheckConstraint(
                name: "ck_published_source",
                table: "Products",
                sql: "NOT \"Published\" OR (\"CommercialSource\" IS NOT NULL AND length(trim(\"CommercialSource\")) >= 5 AND \"CommercialVerifiedOn\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_published_source",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CommercialSource",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CommercialVerifiedOn",
                table: "Products");
        }
    }
}
