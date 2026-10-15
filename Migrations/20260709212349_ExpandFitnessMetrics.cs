using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFitnessMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityLevel",
                table: "BMIRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AgeAtRecord",
                table: "BMIRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BMR",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BodyFatPercentage",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbohydrateGrams",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedWaterLiters",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGrams",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FitnessGoal",
                table: "BMIRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GenderAtRecord",
                table: "BMIRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HipCm",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IdealBodyWeightKg",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeanBodyMassKg",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NeckCm",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGrams",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TDEE",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetCalories",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistCm",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistToHeightRatio",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistToHipRatio",
                table: "BMIRecords",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "AgeAtRecord",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "BMR",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "BodyFatPercentage",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "CarbohydrateGrams",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "EstimatedWaterLiters",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "FatGrams",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "FitnessGoal",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "GenderAtRecord",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "HipCm",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "IdealBodyWeightKg",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "LeanBodyMassKg",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "NeckCm",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "ProteinGrams",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "TDEE",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "TargetCalories",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "WaistCm",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "WaistToHeightRatio",
                table: "BMIRecords");

            migrationBuilder.DropColumn(
                name: "WaistToHipRatio",
                table: "BMIRecords");
        }
    }
}
