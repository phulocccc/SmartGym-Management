using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId",
                table: "ChatbotMessages");

            migrationBuilder.AlterColumn<string>(
                name: "BotResponse",
                table: "ChatbotMessages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<int>(
                name: "MemberId1",
                table: "ChatbotMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_MemberId1",
                table: "ChatbotMessages",
                column: "MemberId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId",
                table: "ChatbotMessages",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId1",
                table: "ChatbotMessages",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId",
                table: "ChatbotMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatbotMessages_MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.AlterColumn<string>(
                name: "BotResponse",
                table: "ChatbotMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId",
                table: "ChatbotMessages",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
