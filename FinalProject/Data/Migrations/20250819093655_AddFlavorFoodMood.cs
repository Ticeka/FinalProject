using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlavorFoodMood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerFlavor_LocalBeers_LocalBeerId",
                table: "LocalBeerFlavor");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerFoodPairing_LocalBeers_LocalBeerId",
                table: "LocalBeerFoodPairing");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerMoodPairing_LocalBeers_LocalBeerId",
                table: "LocalBeerMoodPairing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerMoodPairing",
                table: "LocalBeerMoodPairing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerFoodPairing",
                table: "LocalBeerFoodPairing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerFlavor",
                table: "LocalBeerFlavor");

            migrationBuilder.RenameTable(
                name: "LocalBeerMoodPairing",
                newName: "LocalBeerMoodPairings");

            migrationBuilder.RenameTable(
                name: "LocalBeerFoodPairing",
                newName: "LocalBeerFoodPairings");

            migrationBuilder.RenameTable(
                name: "LocalBeerFlavor",
                newName: "LocalBeerFlavors");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerMoodPairing_LocalBeerId",
                table: "LocalBeerMoodPairings",
                newName: "IX_LocalBeerMoodPairings_LocalBeerId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerFoodPairing_LocalBeerId",
                table: "LocalBeerFoodPairings",
                newName: "IX_LocalBeerFoodPairings_LocalBeerId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerFlavor_LocalBeerId",
                table: "LocalBeerFlavors",
                newName: "IX_LocalBeerFlavors_LocalBeerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerMoodPairings",
                table: "LocalBeerMoodPairings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerFoodPairings",
                table: "LocalBeerFoodPairings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerFlavors",
                table: "LocalBeerFlavors",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerFlavors_LocalBeers_LocalBeerId",
                table: "LocalBeerFlavors",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerFoodPairings_LocalBeers_LocalBeerId",
                table: "LocalBeerFoodPairings",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerMoodPairings_LocalBeers_LocalBeerId",
                table: "LocalBeerMoodPairings",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerFlavors_LocalBeers_LocalBeerId",
                table: "LocalBeerFlavors");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerFoodPairings_LocalBeers_LocalBeerId",
                table: "LocalBeerFoodPairings");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalBeerMoodPairings_LocalBeers_LocalBeerId",
                table: "LocalBeerMoodPairings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerMoodPairings",
                table: "LocalBeerMoodPairings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerFoodPairings",
                table: "LocalBeerFoodPairings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalBeerFlavors",
                table: "LocalBeerFlavors");

            migrationBuilder.RenameTable(
                name: "LocalBeerMoodPairings",
                newName: "LocalBeerMoodPairing");

            migrationBuilder.RenameTable(
                name: "LocalBeerFoodPairings",
                newName: "LocalBeerFoodPairing");

            migrationBuilder.RenameTable(
                name: "LocalBeerFlavors",
                newName: "LocalBeerFlavor");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerMoodPairings_LocalBeerId",
                table: "LocalBeerMoodPairing",
                newName: "IX_LocalBeerMoodPairing_LocalBeerId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerFoodPairings_LocalBeerId",
                table: "LocalBeerFoodPairing",
                newName: "IX_LocalBeerFoodPairing_LocalBeerId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalBeerFlavors_LocalBeerId",
                table: "LocalBeerFlavor",
                newName: "IX_LocalBeerFlavor_LocalBeerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerMoodPairing",
                table: "LocalBeerMoodPairing",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerFoodPairing",
                table: "LocalBeerFoodPairing",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalBeerFlavor",
                table: "LocalBeerFlavor",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerFlavor_LocalBeers_LocalBeerId",
                table: "LocalBeerFlavor",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerFoodPairing_LocalBeers_LocalBeerId",
                table: "LocalBeerFoodPairing",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalBeerMoodPairing_LocalBeers_LocalBeerId",
                table: "LocalBeerMoodPairing",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
