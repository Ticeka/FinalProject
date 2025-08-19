using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalBeers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalBeerFlavor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalBeerId = table.Column<int>(type: "int", nullable: false),
                    Flavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intensity = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalBeerFlavor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalBeerFlavor_LocalBeers_LocalBeerId",
                        column: x => x.LocalBeerId,
                        principalTable: "LocalBeers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalBeerFoodPairing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalBeerId = table.Column<int>(type: "int", nullable: false),
                    FoodName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalBeerFoodPairing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalBeerFoodPairing_LocalBeers_LocalBeerId",
                        column: x => x.LocalBeerId,
                        principalTable: "LocalBeers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalBeerMoodPairing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalBeerId = table.Column<int>(type: "int", nullable: false),
                    Mood = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalBeerMoodPairing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalBeerMoodPairing_LocalBeers_LocalBeerId",
                        column: x => x.LocalBeerId,
                        principalTable: "LocalBeers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalBeerFlavor_LocalBeerId",
                table: "LocalBeerFlavor",
                column: "LocalBeerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalBeerFoodPairing_LocalBeerId",
                table: "LocalBeerFoodPairing",
                column: "LocalBeerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalBeerMoodPairing_LocalBeerId",
                table: "LocalBeerMoodPairing",
                column: "LocalBeerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalBeerFlavor");

            migrationBuilder.DropTable(
                name: "LocalBeerFoodPairing");

            migrationBuilder.DropTable(
                name: "LocalBeerMoodPairing");
        }
    }
}
