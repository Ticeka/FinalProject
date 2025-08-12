using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAlcoholAndPriceToLocalBeer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AlcoholLevel",
                table: "LocalBeers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LocalBeers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacebookPage",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpenHours",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "LocalBeers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LocalBeers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "AlcoholLevel",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "District",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "FacebookPage",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "OpenHours",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LocalBeers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "LocalBeers");
        }
    }
}
