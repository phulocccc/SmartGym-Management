using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PaymentTransactions_PaymentId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_MembershipId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PaymentId",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "PaymentTransactions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "PaymentTransactions",
                newName: "PaymentTransactionId");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "Invoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "IssuedDate",
                table: "Invoices",
                newName: "IssueDate");

            migrationBuilder.RenameColumn(
                name: "InvoiceStatus",
                table: "Invoices",
                newName: "MemberId");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "PaymentTransactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MembershipId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MembershipId",
                table: "PaymentTransactions",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MemberId",
                table: "Invoices",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MembershipId",
                table: "Invoices",
                column: "MembershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Members_MemberId",
                table: "Invoices",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "MembershipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Members_MemberId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_MembershipId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MemberId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MembershipId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PaymentTransactions",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "PaymentTransactionId",
                table: "PaymentTransactions",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Invoices",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "Invoices",
                newName: "InvoiceStatus");

            migrationBuilder.RenameColumn(
                name: "IssueDate",
                table: "Invoices",
                newName: "IssuedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MembershipId",
                table: "PaymentTransactions",
                column: "MembershipId",
                unique: true,
                filter: "[MembershipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentId",
                table: "Invoices",
                column: "PaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PaymentTransactions_PaymentId",
                table: "Invoices",
                column: "PaymentId",
                principalTable: "PaymentTransactions",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
