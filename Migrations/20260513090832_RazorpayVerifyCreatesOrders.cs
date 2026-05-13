using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Note.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RazorpayVerifyCreatesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("""
                UPDATE "Orders"
                SET "PaymentStatus" = 'Paid'
                WHERE "RazorpayPaymentId" IS NOT NULL;
            """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RazorpayPaymentId",
                table: "Orders",
                column: "RazorpayPaymentId",
                unique: true,
                filter: "\"RazorpayPaymentId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_RazorpayPaymentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");
        }
    }
}
