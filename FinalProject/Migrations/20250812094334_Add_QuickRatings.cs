using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Migrations
{
    /// <inheritdoc />
    public partial class Add_QuickRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_LocalBeerId_DeviceId",
                table: "QuickRatings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuickRatings_Score",
                table: "QuickRatings");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "QuickRatings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "QuickRatings");

            migrationBuilder.AlterColumn<string>(
                name: "IpHash",
                table: "QuickRatings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "Fingerprint",
                table: "QuickRatings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_LocalBeerId_IpHash",
                table: "QuickRatings",
                columns: new[] { "LocalBeerId", "IpHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_LocalBeerId_IpHash",
                table: "QuickRatings");

            migrationBuilder.DropColumn(
                name: "Fingerprint",
                table: "QuickRatings");

            migrationBuilder.AlterColumn<string>(
                name: "IpHash",
                table: "QuickRatings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "QuickRatings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "QuickRatings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_LocalBeerId_DeviceId",
                table: "QuickRatings",
                columns: new[] { "LocalBeerId", "DeviceId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuickRatings_Score",
                table: "QuickRatings",
                sql: "[Score] BETWEEN 1 AND 5");
        }
    }
}
