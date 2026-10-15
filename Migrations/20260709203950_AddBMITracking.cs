using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddBMITracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "BMIRecords",
                newName: "RecordedAt");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "BMIRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "BMIRecords");

            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                table: "BMIRecords",
                newName: "CreatedAt");
        }
    }
}
