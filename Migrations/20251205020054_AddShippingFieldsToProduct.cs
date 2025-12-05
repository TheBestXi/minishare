using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShare.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingFieldsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Products",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ShippingMethod",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippingTimeHours",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "ProductRequests",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ShippingMethod",
                table: "ProductRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippingTimeHours",
                table: "ProductRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingMethod",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingTimeHours",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "ProductRequests");

            migrationBuilder.DropColumn(
                name: "ShippingMethod",
                table: "ProductRequests");

            migrationBuilder.DropColumn(
                name: "ShippingTimeHours",
                table: "ProductRequests");
        }
    }
}
