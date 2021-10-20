using Microsoft.EntityFrameworkCore.Migrations;

namespace Forum.Data.Migrations
{
    public partial class CasadeDeleteReplies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostReplies_Posts_PostId",
                table: "PostReplies");

            migrationBuilder.AddForeignKey(
                name: "FK_PostReplies_Posts_PostId",
                table: "PostReplies",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostReplies_Posts_PostId",
                table: "PostReplies");

            migrationBuilder.AddForeignKey(
                name: "FK_PostReplies_Posts_PostId",
                table: "PostReplies",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
