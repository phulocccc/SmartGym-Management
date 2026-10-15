using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Members_MemberId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Attendances");

            migrationBuilder.AddColumn<int>(
                name: "BookingId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClassId",
                table: "Attendances",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Bookings_BookingId",
                table: "Attendances",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_ClassSchedules_ClassId",
                table: "Attendances",
                column: "ClassId",
                principalTable: "ClassSchedules",
                principalColumn: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Members_MemberId",
                table: "Attendances",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Bookings_BookingId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_ClassSchedules_ClassId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Members_MemberId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ClassId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Attendances");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Members_MemberId",
                table: "Attendances",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
