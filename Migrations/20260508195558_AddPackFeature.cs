using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Note.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPackFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPack",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PackSize",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpayOrderId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpayPaymentId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedChoicesJson",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedChoicesJson",
                table: "CartItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PackChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackProductId = table.Column<string>(type: "text", nullable: false),
                    ChoiceProductId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackChoices_Products_ChoiceProductId",
                        column: x => x.ChoiceProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackChoices_Products_PackProductId",
                        column: x => x.PackProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "3",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "4",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "5",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "6",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "7",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "8",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin-user-id",
                columns: new[] { "PasswordResetTokenExpiresAt", "PasswordResetTokenHash" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_PackChoices_ChoiceProductId",
                table: "PackChoices",
                column: "ChoiceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PackChoices_PackProductId",
                table: "PackChoices",
                column: "PackProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackChoices");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPack",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RazorpayOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RazorpayPaymentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SelectedChoicesJson",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SelectedChoicesJson",
                table: "CartItems");
        }
    }
}
