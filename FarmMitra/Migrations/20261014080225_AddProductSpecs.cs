using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmMitra.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Harvested",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinOrder",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Packing",
                table: "Products",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShelfLife",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Products",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Harvested",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Packing",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShelfLife",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Products");
        }
    }
}
