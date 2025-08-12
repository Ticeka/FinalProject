using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAlcoholDetailToLocalBeer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "LocalBeers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Award",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Creator",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Distributor",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DistributorChanel",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MainIngredients",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfOrigin",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductMethod",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductYear",
                table: "LocalBeers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rights",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TypeOfLiquor",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "LocalBeers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Award",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Creator",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Distributor",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "DistributorChanel",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "MainIngredients",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "PlaceOfOrigin",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "ProductMethod",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "ProductYear",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Rights",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "TypeOfLiquor",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "LocalBeers");
        }
    }
}
