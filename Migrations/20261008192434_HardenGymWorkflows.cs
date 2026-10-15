using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class HardenGymWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop for manual review rather than silently deleting or rewriting legacy data.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM ChatbotMessages WHERE MemberId1 IS NOT NULL AND MemberId1 <> MemberId)
                    THROW 51001, 'Review conflicting chat member relationships before migration.', 1;
                IF EXISTS (SELECT BookingId FROM Attendances GROUP BY BookingId HAVING COUNT(*) > 1)
                    THROW 51002, 'Review duplicate attendance records before migration.', 1;
                IF EXISTS (SELECT MemberId, ClassId FROM Bookings WHERE Status IN (0, 2) GROUP BY MemberId, ClassId HAVING COUNT(*) > 1)
                    THROW 51003, 'Review duplicate reserved bookings before migration.', 1;
                IF EXISTS (SELECT InvoiceNumber FROM Invoices GROUP BY InvoiceNumber HAVING COUNT(*) > 1)
                    THROW 51004, 'Review duplicate invoice numbers before migration.', 1;
                IF EXISTS (SELECT TransactionCode FROM PaymentTransactions WHERE TransactionCode IS NOT NULL GROUP BY TransactionCode HAVING COUNT(*) > 1)
                    THROW 51005, 'Review duplicate payment references before migration.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatbotMessages_MemberId",
                table: "ChatbotMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatbotMessages_MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ClassId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MemberId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BMIRecords_MemberId",
                table: "BMIRecords");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "ChatbotMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchasedDurationDays",
                table: "Memberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchasedPackageName",
                table: "Memberships",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasedPrice",
                table: "Memberships",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TransactionCode",
                table: "PaymentTransactions",
                column: "TransactionCode",
                unique: true,
                filter: "[TransactionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSchedules_Status_StartTime",
                table: "ClassSchedules",
                columns: new[] { "Status", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_MemberId_CreatedAt",
                table: "ChatbotMessages",
                columns: new[] { "MemberId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ClassId_Status",
                table: "Bookings",
                columns: new[] { "ClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MemberId_ClassId",
                table: "Bookings",
                columns: new[] { "MemberId", "ClassId" },
                unique: true,
                filter: "[Status] IN (0, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_BMIRecords_MemberId_RecordedAt",
                table: "BMIRecords",
                columns: new[] { "MemberId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_TransactionCode",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_ClassSchedules_Status_StartTime",
                table: "ClassSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ChatbotMessages_MemberId_CreatedAt",
                table: "ChatbotMessages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ClassId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MemberId_ClassId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BMIRecords_MemberId_RecordedAt",
                table: "BMIRecords");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PurchasedDurationDays",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "PurchasedPackageName",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "PurchasedPrice",
                table: "Memberships");

            migrationBuilder.AddColumn<int>(
                name: "MemberId1",
                table: "ChatbotMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_MemberId",
                table: "ChatbotMessages",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_MemberId1",
                table: "ChatbotMessages",
                column: "MemberId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ClassId",
                table: "Bookings",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MemberId",
                table: "Bookings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BMIRecords_MemberId",
                table: "BMIRecords",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_BookingId",
                table: "Attendances",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotMessages_Members_MemberId1",
                table: "ChatbotMessages",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "MemberId");
        }
    }
}
