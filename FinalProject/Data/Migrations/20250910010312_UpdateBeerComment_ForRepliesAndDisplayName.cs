using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBeerComment_ForRepliesAndDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId1",
                table: "BeerComments");

            migrationBuilder.DropIndex(
                name: "IX_BeerComments_UserId1",
                table: "BeerComments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "BeerComments");

            migrationBuilder.RenameColumn(
                name: "UserRating",
                table: "BeerComments",
                newName: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "BeerComments",
                newName: "UserRating");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "BeerComments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeerComments_UserId1",
                table: "BeerComments",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BeerComments_AspNetUsers_UserId1",
                table: "BeerComments",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
