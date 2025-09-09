using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Pairing_FieldNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserStats_StatsUserId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StatsUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StatsUserId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "Reviews",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Favorites",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Comments",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Badges",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "QuickRatings",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "QuickRatings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Province",
                table: "LocalBeers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LocalBeers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "LocalBeerMoodPairings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mood",
                table: "LocalBeerMoodPairings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "LocalBeerFoodPairings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FoodName",
                table: "LocalBeerFoodPairings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Flavor",
                table: "LocalBeerFlavors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BeerComments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "BeerComments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_LocalBeerId",
                table: "QuickRatings",
                column: "LocalBeerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_LocalBeerId_UserId",
                table: "QuickRatings",
                columns: new[] { "LocalBeerId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_UserId",
                table: "QuickRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickRatings_UserId1",
                table: "QuickRatings",
                column: "UserId1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuickRatings_Score",
                table: "QuickRatings",
                sql: "[Score] >= 0 AND [Score] <= 5");

            migrationBuilder.CreateIndex(
                name: "IX_LocalBeers_Name",
                table: "LocalBeers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LocalBeers_Province",
                table: "LocalBeers",
                column: "Province");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LocalBeerFlavor_Intensity",
                table: "LocalBeerFlavors",
                sql: "[Intensity] >= 0 AND [Intensity] <= 1");

            migrationBuilder.CreateIndex(
                name: "IX_BeerComments_LocalBeerId",
                table: "BeerComments",
                column: "LocalBeerId");

            migrationBuilder.CreateIndex(
                name: "IX_BeerComments_UserId",
                table: "BeerComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BeerComments_UserId1",
                table: "BeerComments",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId",
                table: "BeerComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId1",
                table: "BeerComments",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BeerComments_LocalBeers_LocalBeerId",
                table: "BeerComments",
                column: "LocalBeerId",
                principalTable: "LocalBeers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickRatings_AspNetUsers_UserId",
                table: "QuickRatings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickRatings_AspNetUsers_UserId1",
                table: "QuickRatings",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId",
                table: "BeerComments");

            migrationBuilder.DropForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId1",
                table: "BeerComments");

            migrationBuilder.DropForeignKey(
                name: "FK_BeerComments_LocalBeers_LocalBeerId",
                table: "BeerComments");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickRatings_AspNetUsers_UserId",
                table: "QuickRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickRatings_AspNetUsers_UserId1",
                table: "QuickRatings");

            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_LocalBeerId",
                table: "QuickRatings");

            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_LocalBeerId_UserId",
                table: "QuickRatings");

            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_UserId",
                table: "QuickRatings");

            migrationBuilder.DropIndex(
                name: "IX_QuickRatings_UserId1",
                table: "QuickRatings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuickRatings_Score",
                table: "QuickRatings");

            migrationBuilder.DropIndex(
                name: "IX_LocalBeers_Name",
                table: "LocalBeers");

            migrationBuilder.DropIndex(
                name: "IX_LocalBeers_Province",
                table: "LocalBeers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LocalBeerFlavor_Intensity",
                table: "LocalBeerFlavors");

            migrationBuilder.DropIndex(
                name: "IX_BeerComments_LocalBeerId",
                table: "BeerComments");

            migrationBuilder.DropIndex(
                name: "IX_BeerComments_UserId",
                table: "BeerComments");

            migrationBuilder.DropIndex(
                name: "IX_BeerComments_UserId1",
                table: "BeerComments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "QuickRatings");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "BeerComments");

            migrationBuilder.AlterColumn<int>(
                name: "Reviews",
                table: "UserStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Favorites",
                table: "UserStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Comments",
                table: "UserStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Badges",
                table: "UserStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "QuickRatings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Province",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LocalBeers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "LocalBeerMoodPairings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mood",
                table: "LocalBeerMoodPairings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "LocalBeerFoodPairings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FoodName",
                table: "LocalBeerFoodPairings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Flavor",
                table: "LocalBeerFlavors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BeerComments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatsUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

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
    }
}
