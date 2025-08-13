using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatsAndBeerShapes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatsUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reviews = table.Column<int>(type: "int", nullable: false),
                    Favorites = table.Column<int>(type: "int", nullable: false),
                    Badges = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStats_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StatsUserId",
                table: "AspNetUsers",
                column: "StatsUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserStats_StatsUserId",
                table: "AspNetUsers",
                column: "StatsUserId",
                principalTable: "UserStats",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserStats_StatsUserId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StatsUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StatsUserId",
                table: "AspNetUsers");
        }
    }
}
